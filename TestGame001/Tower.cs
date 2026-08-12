using System;
using Microsoft.Xna.Framework;

namespace TestGame001
{
    // Base type for every placeable tower. Never instantiated directly - always via a concrete
    // subclass, each of which fixes its own combat stats.
    public abstract class Tower
    {
        // Grid-snapped world position (top-left corner of the tile it occupies). Set once at
        // construction and never moved afterward.
        public Vector2 Position { get; }

        // How long since this tower last fired; compared against Cooldown each frame to decide
        // when it's allowed to shoot again.
        public TimeSpan TimeSinceLastShot { get; set; } = TimeSpan.Zero;

        // Which enemy this tower prioritizes when more than one is in range. Player-selectable.
        public TargetingMode TargetingMode { get; set; } = TargetingMode.ClosestToTower;

        // Combat stats - fixed per concrete tower type, overridden by each subclass.
        public abstract TimeSpan Cooldown { get; }
        public abstract float Range { get; }
        public abstract float Damage { get; }
        public abstract float BulletSpeed { get; }

        protected Tower(Vector2 position)
        {
            Position = position;
        }

        // World-space center of this tower's tile - used for range checks and as the bullet spawn point.
        public Vector2 GetCenter() =>
            Position + new Vector2(GameConstants.GridSize / 2f, GameConstants.GridSize / 2f);
    }

    // Cheap, fast-firing, short-range tower - the default starting option.
    public class BasicTower : Tower
    {
        public override TimeSpan Cooldown => TimeSpan.FromSeconds(1);
        public override float Range => 150f;
        public override float Damage => 10f;
        public override float BulletSpeed => 1200f;

        public BasicTower(Vector2 position) : base(position) { }
    }

    // Slow-firing, long-range, high-damage tower.
    public class SniperTower : Tower
    {
        public override TimeSpan Cooldown => TimeSpan.FromSeconds(2.5);
        public override float Range => 350f;
        public override float Damage => 60f;
        public override float BulletSpeed => 2400f;

        public SniperTower(Vector2 position) : base(position) { }
    }
}