using System;

namespace SnowLib.Types
{
    public class Location
    {
        public Direction Facing;
        public DateTime LastActive;
        public ushort X;
        public ushort Y;

        public override string ToString()
        {
            return X + "," + Y + " Direction: " + Facing;
        }
    }
}