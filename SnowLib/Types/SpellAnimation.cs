using System;

namespace SnowLib.Types
{
    public class SpellAnimation
    {
        public uint CastedFrom;
        public uint CastedTo;
        public ushort Number;
        public uint Speed;
        public DateTime Time;

        public SpellAnimation(uint To, uint From, ushort number, uint Speed = 100)
        {
            Time = DateTime.Now;

            CastedTo = To;
            CastedFrom = From;
            Number = number;
            this.Speed = Speed;
        }
    }
}