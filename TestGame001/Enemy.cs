using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TestGame001
{
    // Base type for every enemy that walks the path. Never instantiated directly - always via a
    // concrete subclass, each of which fixes its own health/speed.
    public abstract class Enemy
    {

        public string Name { get; }
        // Top-left world position, updated every frame as the enemy moves along the path.
        public Vector2 Position;

        // False once health drops to 0 or the enemy reaches the exit; queued for removal.
        public bool IsActive = true;

        // Index into the path's waypoint list of the waypoint this enemy is currently walking toward.
        public int CurrentWaypointIndex = 0;

        // Movement/health stats - fixed per concrete enemy type, set once via the constructor.
        public int MaxHealth { get; }
        public float Speed { get; }
        public Texture2D Texture { get; }

        // Current remaining health; ticks down as bullets land.
        public float Health { get; set; }

        protected Enemy(Vector2 startPosition, int maxHealth, float speed, Texture2D texture)
        {
            Name = MonsterNameGenerator.Generate();
            Position = startPosition;
            MaxHealth = maxHealth;
            Health = maxHealth;
            Speed = speed;
            Texture = texture;
        }

        // World-space center of this enemy's tile - used for range checks, targeting, and bullet collision.
        public Vector2 GetCenter() =>
            Position + new Vector2(GameConstants.GridSize / 2f, GameConstants.GridSize / 2f);

        // Distance remaining from this enemy's current position to the end of the path - the
        // distance to its next waypoint, plus every full segment after that. Used by towers set
        // to "closest to exit" targeting.
        public float GetRemainingDistance(List<Vector2> path)
        {
            if (CurrentWaypointIndex >= path.Count) return 0f;

            float remaining = Vector2.Distance(Position, path[CurrentWaypointIndex]);
            for (int i = CurrentWaypointIndex; i < path.Count - 1; i++)
            {
                remaining += Vector2.Distance(path[i], path[i + 1]);
            }
            return remaining;
        }

        // Applies bullet damage and deactivates the enemy once health is depleted.
        public virtual void TakeDamage(float amount)
        {
            Health -= amount;
            if (Health <= 0) IsActive = false;
        }
    }

    // Standard enemy - moderate health, moderate speed.
    public class BasicEnemy : Enemy
    {
        public BasicEnemy(Vector2 startPosition, Texture2D texture)
            : base(startPosition, maxHealth: 50, speed: 2f, texture) { }
    }

    // Low health, high speed.
    public class FastEnemy : Enemy
    {
        public FastEnemy(Vector2 startPosition, Texture2D texture)
            : base(startPosition, maxHealth: 50, speed: 4f, texture) { }
    }

    // High health, low speed.
    public class TankEnemy : Enemy
    {
        public TankEnemy(Vector2 startPosition, Texture2D texture)
            : base(startPosition, maxHealth: 300, speed: 1f, texture) { }
    }
}