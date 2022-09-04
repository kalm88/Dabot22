namespace SnowLib.Types
{
    public class SpriteAnimation
    {
        public SpriteAnimation(uint _Serial, byte _Animation)
        {
            Serial = _Serial;
            Animation = _Animation;
        }

        public uint Serial { get; set; }
        public byte Animation { get; set; }
    }
}