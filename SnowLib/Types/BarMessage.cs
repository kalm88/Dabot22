using System;

namespace SnowLib.Types
{
    public class BarMessage
    {
        public DateTime Date;
        public string Message;
        public byte Type;

        public BarMessage(byte type, string message)
        {
            Type = type;
            Message = message;
            Date = DateTime.Now;
        }

        public TimeSpan TimeElapsed => DateTime.Now - Date;
    }
}