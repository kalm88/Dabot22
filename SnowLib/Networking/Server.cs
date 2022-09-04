using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SnowLib.Networking
{
    public delegate bool ClientMessageHandler(Client client, ClientPacket msg);

    public delegate bool ServerMessageHandler(Client client, ServerPacket msg);

    public delegate void OnClientConnection(Client client);

    public interface IProxyServer
    {
        List<Client> Clients { get; set; }
        ClientMessageHandler[] ClientMessageHandlers { get; set; }
        ServerMessageHandler[] ServerMessageHandlers { get; set; }
        void SetEndPoint(IPEndPoint ipEndPoint);
        OnClientConnection OnConnectedClient { get; set; }
        OnClientConnection OnRemovedClient { get; set; }
    }

    public class Server : IProxyServer
    {
        private readonly string ip;
        private TcpListener Listener { get; set; }
        private Thread ServerLoopThread { get; set; }
        private EndPoint RemoteEndPoint { get; set; }

        public ClientMessageHandler[] ClientMessageHandlers { get; set; } = new ClientMessageHandler[256];
        public ServerMessageHandler[] ServerMessageHandlers { get; set; } = new ServerMessageHandler[256];
        public List<Client> Clients { get; set; } = new List<Client>();
        public bool Running { get; set; }

        public OnClientConnection OnConnectedClient { get; set; }
        public OnClientConnection OnRemovedClient { get; set; }

        public void SetEndPoint(IPEndPoint ipEndPoint)
        {
            RemoteEndPoint = ipEndPoint ?? throw new ArgumentNullException(nameof(ipEndPoint));
        }

        public Server(string ip, int port = 2610)
        {
            this.ip = ip ?? throw new ArgumentNullException(nameof(ip));
            RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            BindMessageHandlers();
            StartListening();

            void BindMessageHandlers()
            {
                for (var i = 0; i < 256; i++)
                {
                    ClientMessageHandlers[i] = (client, msg) => true;
                    ServerMessageHandlers[i] = (client, msg) => true;
                }
            }

            void StartListening()
            {
                Listener = new TcpListener(IPAddress.Loopback, 2610);
                Listener.Start(10);

                ServerLoopThread = new Thread(ServerLoop);
                ServerLoopThread.Start();
            }
        }

        public void ServerLoop()
        {
            Running = true;

            while (Running)
            {
                if (Listener.Pending()) Listener.BeginAcceptSocket(EndConnectClient, Listener);

                Thread.Sleep(500);
            }
        }

        private void EndConnectClient(IAsyncResult ar)
        {
            var state = (TcpListener) ar.AsyncState;
            var socket = state.EndAcceptSocket(ar);
            var client = new Client(this, socket, RemoteEndPoint);

            Clients.Add(client);
            {
                RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), 2610);
                Listener.BeginAcceptSocket(EndConnectClient, Listener);
            }
        }
    }
}