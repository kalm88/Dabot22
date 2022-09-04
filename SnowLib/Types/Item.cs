using System;

namespace SnowLib.Types
{
    public class Item
    {
        public byte Slot { get; set; }
        public ushort IconSet { get; set; }
        public byte Icon { get; set; }
        public string Name { get; set; }
        public uint Amount { get; set; }
        public bool Stackable { get; set; }
        public uint CurrentDurability { get; set; }
        public uint MaximumDurability { get; set; }

        public bool Equipped { get; set; }
        public DateTime LastUsed { get; set; }
    }
}