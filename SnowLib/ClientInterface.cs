using System.Windows.Forms;
using SnowLib.Networking;

namespace SnowLib
{
    public class ClientInterface : IClientInterface
    {
        private readonly Client client;

        public ClientInterface(Client clientInterface)
        {
            client = clientInterface;
        }

        public Control Tab { get; set; }
    }
}