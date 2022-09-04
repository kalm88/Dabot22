using SnowLib.Networking;

namespace SnowLib.Scripting
{
    public abstract class Script
    {
        public bool Running;
        public Client client;

        public virtual void OnMessage(string msg)
        {
        }

        public abstract void Start();
        public abstract void Stop();
    }
}