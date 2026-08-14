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
        public Color Tint;

        // Current world position, advanced each Update call.
        public Vector2 Position;

        // Fixed heading set once in the constructor; never changes after that (no homing/steering).
        public Vector2 Direction;

        // Pixels per second.
        public float Speed;

        public float Damage;

        // How close (in pixels) the bullet needs to be to an enemy's center to register a hit.
        public float HitRadius = 16f * GameConstants.EnemyScale;

        // Total distance this bullet can travel before it expires with no hit (matches the firing tower's range).
        public float MaxRange;

        // False once the bullet has either hit something or traveled its max range; queued for removal.
        public bool IsActive = true;

        // Distance traveled so far this bullet's lifetime, used to check against MaxRange.
        private float distanceTraveled = 0f;

        public Bullet(Vector2 startPosition, Vector2 initialAimPoint, float damage, float speed, float maxRange, Color tint)
        {
            Position = startPosition;
            Damage = damage;
            Speed = speed;
            MaxRange = maxRange;
            Tint = tint;

            // Aim once, at spawn time, toward wherever the target was when the shot was fired.
            Vector2 toAimPoint = initialAimPoint - startPosition;
            Direction = toAimPoint.LengthSquared() > 0f ? Vector2.Normalize(toAimPoint) : Vector2.Zero;
        }

        // Advances the bullet along its fixed direction for this frame, then checks for a hit.
        // Returns the enemy that was hit this frame, or null if nothing was hit (yet, or ever).
        public Enemy Update(List<Enemy> enemies, float deltaSeconds)
        {
            Vector2 previousPosition = Position;
            float moveDistance = Speed * deltaSeconds;
            Position += Direction * moveDistance;
            distanceTraveled += moveDistance;

            // Check the entire path traveled this frame (not just the endpoint) against every active
            // enemy, so fast bullets can't skip past a thin target between frames.
            foreach (var enemy in enemies)
            {
                if (!enemy.IsActive) continue;

                Vector2 closestPoint = ClosestPointOnSegment(previousPosition, Position, enemy.GetCenter());
                if (Vector2.Distance(closestPoint, enemy.GetCenter()) <= HitRadius)
                {
                    Position = closestPoint; // move the bullet to the hit point for visual effect
                    IsActive = false;
                    return enemy;
                }
            }

            if (distanceTraveled >= MaxRange)
            {
                IsActive = false;
            }

            return null;
        }

        // Finds the closest point on segment a-b to point p, clamped to the segment itself
        // (not the infinite line through a and b).
        private static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float lengthSquared = ab.LengthSquared();
            if (lengthSquared < 0.0001f) return a; // a and b are the same point - no movement this frame

            float t = Vector2.Dot(p - a, ab) / lengthSquared;
            t = MathHelper.Clamp(t, 0f, 1f);
            return a + ab * t;
        }
    }
}