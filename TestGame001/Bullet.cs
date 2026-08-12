using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TestGame001
{
    // A straight-line, non-homing projectile. It's aimed once at spawn time toward wherever its
    // target was at that instant, then keeps flying in that exact direction — it does not track
    // or steer toward the target afterward. It deals damage to whatever active enemy it first
    // comes within HitRadius of, and expires once it's traveled MaxRange without hitting anything.
    public class Bullet
    {
        // Current world position, advanced each Update call.
        public Vector2 Position;

        // Fixed heading set once in the constructor; never changes after that (no homing/steering).
        public Vector2 Direction;

        // Pixels per second.
        public float Speed;

        public float Damage;

        // How close (in pixels) the bullet needs to be to an enemy's center to register a hit.
        public float HitRadius = 16f;

        // Total distance this bullet can travel before it expires with no hit (matches the firing tower's range).
        public float MaxRange;

        // False once the bullet has either hit something or traveled its max range; queued for removal.
        public bool IsActive = true;

        // Distance traveled so far this bullet's lifetime, used to check against MaxRange.
        private float distanceTraveled = 0f;

        public Bullet(Vector2 startPosition, Vector2 initialAimPoint, float damage, float speed, float maxRange)
        {
            Position = startPosition;
            Damage = damage;
            Speed = speed;
            MaxRange = maxRange;

            // Aim once, at spawn time, toward wherever the target was when the shot was fired.
            Vector2 toAimPoint = initialAimPoint - startPosition;
            Direction = toAimPoint.LengthSquared() > 0f ? Vector2.Normalize(toAimPoint) : Vector2.Zero;
        }

        // Advances the bullet along its fixed direction for this frame, then checks for a hit.
        // Returns the enemy that was hit this frame, or null if nothing was hit (yet, or ever).
        public Enemy Update(List<Enemy> enemies, float deltaSeconds)
        {
            float moveDistance = Speed * deltaSeconds;
            Position += Direction * moveDistance;
            distanceTraveled += moveDistance;

            // Check every active enemy's current position against the bullet's new position -
            // this is a genuine per-frame collision check, not just "did I reach my original target".
            foreach (var enemy in enemies)
            {
                if (enemy.IsActive && Vector2.Distance(Position, enemy.GetCenter()) <= HitRadius)
                {
                    IsActive = false;
                    return enemy;
                }
            }

            // Traveled as far as this tower's range allows with nothing hit - the shot missed.
            if (distanceTraveled >= MaxRange)
            {
                IsActive = false;
            }

            return null;
        }
    }
}