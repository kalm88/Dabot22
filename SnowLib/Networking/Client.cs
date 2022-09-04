using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SnowLib.Networking
{
    public delegate void OnClientCallBack(Server server, Client client, IClientInterface clientInterface);

    public delegate void OnMessageCallBack(Server server, Client client, Packet message);

    public class Client : INotifyPropertyChanged
    {
        private const string EncryptionNamekey = "UrkcnItnI";
        private readonly object syncObject = new object();

        public byte Seed;
        public byte[] Key, KeyTable;

        public Server Server { get; }
        public IClientInterface Interface { get; set; }
        public Thread ClientLoopThread { get; }
        public uint Serial { get; set; }

        public Socket ClientSocket, ServerSocket;

        private bool clientReceiving, serverReceiving;

        public event PropertyChangedEventHandler PropertyChanged;

        private bool connected;

        public bool Connected
        {
            get => connected;
            set
            {
                connected = value;
                ConnectionChanged();
            }
        }

        protected void ConnectionChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private readonly byte[]
            clientBuffer = new byte[65535],
            serverBuffer = new byte[65535];

        private readonly List<byte>
            fullClientBuffer = new List<byte>(),
            fullServerBuffer = new List<byte>();

        private byte clientOrdinal, serverOrdinal;

        private readonly Queue<ServerPacket>
            clientSendQueue = new Queue<ServerPacket>(),
            serverProcessQueue = new Queue<ServerPacket>();

        private readonly Queue<ClientPacket>
            serverSendQueue = new Queue<ClientPacket>(),
            clientProcessQueue = new Queue<ClientPacket>();

        public event OnClientCallBack OnClientRemoved;
        public event OnMessageCallBack OnMessageReceived, OnMessageSent;

        public Client(Server server, Socket socket, EndPoint endPoint)
        {
            Server = server;
            ClientSocket = socket;
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.Connect(endPoint);
            Interface = new ClientInterface(this);

            Key = Encoding.ASCII.GetBytes(EncryptionNamekey);
            KeyTable = new byte[1024];

            ClientLoopThread = new Thread(ClientLoop);
            ClientLoopThread.Start();
        }

        public void ClientLoop()
        {
            connected = true;

            while (connected)
            {
                lock (syncObject)
                {
                    try
                    {
                        ClientReceive();
                        ClientProcess();
                        ClientDequeue();

                        ServerReceive();
                        ServerProcess();
                        ServerDequeue();
                    }
                    catch
                    {
                        connected = false;
                    }
                }

                Task.Delay(1);
            }

            lock (syncObject)
            {
                if (Server.Clients.Remove(this)) OnClientRemoved?.Invoke(Server, this, Interface);
            }
        }

        #region Networking

        public void ClientReceive()
        {
            if (!connected || clientReceiving)
                return;


            clientReceiving = true;
            ClientSocket.BeginReceive(clientBuffer, 0, clientBuffer.Length, SocketFlags.None, ClientEndReceive,
                ClientSocket);
        }

        public void ServerReceive()
        {
            if (!connected || serverReceiving)
                return;

            serverReceiving = true;
            ServerSocket.BeginReceive(serverBuffer, 0, serverBuffer.Length, SocketFlags.None, ServerEndReceive,
                ServerSocket);
        }

        public void ClientProcess()
        {
            lock (syncObject)
            {
                while (clientProcessQueue.Count > 0)
                {
                    var msg = clientProcessQueue.Dequeue();
                    if (!Server.ClientMessageHandlers[msg.Opcode].Invoke(this, msg))
                        continue;

                    Enqueue(msg);
                    OnMessageSent?.Invoke(Server, this, msg);
                }
            }
        }

        public void ServerProcess()
        {
            lock (syncObject)
            {
                while (serverProcessQueue.Count > 0)
                {
                    var msg = serverProcessQueue.Dequeue();
                    if (!Server.ServerMessageHandlers[msg.Opcode].Invoke(this, msg))
                        continue;

                    Enqueue(msg);
                    OnMessageReceived?.Invoke(Server, this, msg);
                }
            }
        }

        public void ClientDequeue()
        {
            lock (syncObject)
            {
                while (clientSendQueue.Count > 0)
                {
                    var msg = clientSendQueue.Dequeue();

                    if (msg.ShouldEncrypt)
                    {
                        msg.Ordinal = clientOrdinal++;
                        msg.Encrypt(this);
                    }

                    msg.Length = (ushort) (msg.BodyData.Length + (msg.Header.Length - 3));
                    var buffer = new byte[msg.Header.Length + msg.BodyData.Length];
                    Array.Copy(msg.Header, 0, buffer, 0, msg.Header.Length);
                    Array.Copy(msg.BodyData, 0, buffer, msg.Header.Length, msg.BodyData.Length);

                    ClientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, ClientEndSend, this);
                }
            }
        }

        public void ServerDequeue()
        {
            lock (syncObject)
            {
                while (serverSendQueue.Count > 0)
                {
                    var msg = serverSendQueue.Dequeue();

                    if (msg.Opcode == 0x39 || msg.Opcode == 0x3A) msg.EncryptDialog();

                    if (msg.ShouldEncrypt)
                    {
                        msg.Ordinal = serverOrdinal++;
                        msg.Encrypt(this);
                    }

                    msg.Length = (ushort) (msg.BodyData.Length + (msg.Header.Length - 3));
                    var buffer = new byte[msg.Header.Length + msg.BodyData.Length];
                    Array.Copy(msg.Header, 0, buffer, 0, msg.Header.Length);
                    Array.Copy(msg.BodyData, 0, buffer, msg.Header.Length, msg.BodyData.Length);

                    ServerSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, ServerEndSend, this);
                }
            }
        }

        public void Enqueue(ClientPacket msg)
        {
            lock (syncObject)
            {
                serverSendQueue.Enqueue(msg);
            }
        }

        public void Enqueue(ServerPacket msg)
        {
            lock (syncObject)
            {
                clientSendQueue.Enqueue(msg);
            }
        }

        private void ClientEndSend(IAsyncResult ar)
        {
            var client = (Client) ar.AsyncState;
            client.ClientSocket.EndSend(ar, out var error);

            if (error != SocketError.Success) client.connected = false;
        }

        private void ServerEndSend(IAsyncResult ar)
        {
            var client = (Client) ar.AsyncState;
            client.ServerSocket.EndSend(ar, out var error);

            if (error != SocketError.Success) client.connected = false;
        }

        private void ClientEndReceive(IAsyncResult ar)
        {
            ClientSocket = (Socket) ar.AsyncState;

            try
            {
                var count = ClientSocket.EndReceive(ar);

                for (var i = 0; i < count; i++)
                    fullClientBuffer.Add(clientBuffer[i]);

                if (count == 0 || fullClientBuffer[0] != 0xAA)
                {
                    connected = false;
                    return;
                }

                while (fullClientBuffer.Count > 3)
                {
                    var length = (fullClientBuffer[1] << 8) + fullClientBuffer[2] + 3;

                    if (length > fullClientBuffer.Count)
                        break;

                    var data = fullClientBuffer.GetRange(0, length);
                    fullClientBuffer.RemoveRange(0, length);

                    var msg = new ClientPacket(data.ToArray());

                    if (msg.ShouldEncrypt) msg.Decrypt(this);

                    if (msg.Opcode == 0x39 || msg.Opcode == 0x3A) msg.DecryptDialog();

                    lock (syncObject)
                    {
                        clientProcessQueue.Enqueue(msg);
                    }
                }

                clientReceiving = false;
            }
            catch
            {
                connected = false;
            }
            finally
            {
                ClientReceive();
            }
        }

        private void ServerEndReceive(IAsyncResult ar)
        {
            ServerSocket = (Socket) ar.AsyncState;

            try
            {
                var count = ServerSocket.EndReceive(ar, out var error);

                if (error != SocketError.Success)
                {
                    connected = false;
                    return;
                }

                for (var i = 0; i < count; i++)
                    fullServerBuffer.Add(serverBuffer[i]);

                if (count == 0 || fullServerBuffer[0] != 0xAA)
                {
                    connected = false;
                    return;
                }

                while (fullServerBuffer.Count > 3)
                {
                    var length = (fullServerBuffer[1] << 8) + fullServerBuffer[2] + 3;

                    if (length > fullServerBuffer.Count)
                        break;

                    var data = fullServerBuffer.GetRange(0, length);
                    fullServerBuffer.RemoveRange(0, length);

                    var msg = new ServerPacket(data.ToArray());

                    if (msg.ShouldEncrypt) msg.Decrypt(this);

                    lock (syncObject)
                    {
                        serverProcessQueue.Enqueue(msg);
                    }
                }

                serverReceiving = false;
            }
            catch
            {
                connected = false;
            }
            finally
            {
                ServerReceive();
            }
        }

        #endregion

        public byte[] GenerateKey(ushort bRand, byte sRand)
        {
            var key = new byte[9];

            for (var i = 0; i < 9; ++i) key[i] = KeyTable[(i * (9 * i + sRand * sRand) + bRand) % 1024];

            return key;
        }
    }
}