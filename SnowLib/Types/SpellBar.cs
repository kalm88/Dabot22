namespace SnowLib.Types
{
    public class SpellBar
    {
        public enum IconColor
        {
            White = 0x06,
            Red = 0x05,
            Orange = 0x04,
            Yellow = 0x03,
            Green = 0x02,
            Blue = 0x01,
            Gone = 0x00
        }

        public IconColor Color;
        public ushort Icon;

        public SpellBar(ushort icon, byte color)
        {
            Icon = icon;
            Color = (IconColor) color;
        }
    }
}