using System;

namespace SnowLib.Types
{
    public class Spell
    {
        public byte Lines;
        public string Name;
        public byte Slot;
        public SpellType type;

        public Spell(string name, byte slot, byte lines)
        {
            Name = name;
            Slot = slot;
            Lines = lines;
        }
    }

    public class SKill
    {
        public SKill(string _name, byte _slot, ushort _icon)
        {
            Name = _name;
            Slot = _slot;
            Icon = _icon;
        }

        public byte Slot { get; set; }
        public ushort Icon { get; set; }
        public string Name { get; set; }
        public DateTime LastUsed { get; set; }
    }
}