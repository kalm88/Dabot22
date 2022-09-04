using System;
using System.Collections.Generic;

namespace SnowLib.Types
{
    public class EntityArgs : EventArgs
    {
        public EntityArgs(Entity entity, SpellAnimation animation)
        {
            Time = DateTime.Now;
            Entity = entity;
            Animation = animation;
        }

        public Entity Entity { get; set; }
        public SpellAnimation Animation { get; set; }
        public DateTime Time { get; set; }

        public TimeSpan Elapsed => DateTime.Now - Time;
    }

    public class Entity : IComparable<Entity>
    {
        public bool Active;
        public Dictionary<ushort, SpellAnimation> Animations = new Dictionary<ushort, SpellAnimation>();
        public bool Cursed;

        public DateTime DateAdded;
        public DateTime DateEvent;
        public DateTime DateTargeted;
        public DateTime DateUpdated;
        public byte HPPercent;
        public DateTime LastAttacked;

        public Spell LastUserCastedSpell = default;
        public string Name;
        public Location Position = new Location();
        public uint Serial;
        public ushort SpriteID;
        public int TimesHit;
        public EntityType Type;
        public bool fased;

        public Entity(bool RaiseEvents = true)
        {
            DateAdded = DateTime.Now;

            if (RaiseEvents) OnSpellAnimation += OnAnimation;
        }

        //entity animation logic, this is used to identify with the server what spells were cast on monsters
        public EventHandler<EntityArgs> OnSpellAnimation { get; set; }

        public DateTime LowHpTime { get; set; }

        public int CompareTo(Entity other)
        {
            var Temp = other;

            if (Serial < Temp.Serial)
                return 1;
            if (Serial > Temp.Serial)
                return -1;

            return 0;
        }

        public void EntityTargeted(Spell spell)
        {
            DateTargeted = DateTime.Now;
            LastUserCastedSpell = spell;
        }

        public void Update()
        {
            DateUpdated = DateTime.Now;
        }

        public void OnAnimation(object sender, EntityArgs args)
        {
            if (args.Animation.Number == 0x0111)
                fased = true;
            if (args.Animation.Number == 0x0101)
                Cursed = true;
            if (args.Animation.Number == 0x0068)
                Cursed = true;
            if (args.Animation.Number == 243)
                Cursed = true;
            if (args.Animation.Number == 82)
                Cursed = true;


            if (!Animations.ContainsKey(args.Animation.Number))
            {
                DateEvent = DateTime.Now;
                Animations.Add(args.Animation.Number, args.Animation);
            }
            else
            {
                Animations[args.Animation.Number].Time = DateTime.Now;
            }
        }

        public void Clear()
        {
            Animations.Clear();
        }
    }
}