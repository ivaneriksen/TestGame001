using System.Collections.Generic;

namespace TestGame001
{
    // Describes what a single wave spawns - just data, no behavior. Built either by the
    // procedural generator or a hand-authored override (e.g. a boss wave).
    public class Wave
    {
        // Ordered list of enemy types to spawn this wave. For now, every entry is BasicEnemy -
        // once more enemy types exist, this list is what lets waves mix them.
        public List<EnemyType> EnemySpawns { get; } = new List<EnemyType>();
    }

    // Identifies which concrete Enemy subclass to spawn, without WaveManager needing to know
    // about concrete types directly.
    public enum EnemyType
    {
        Basic
        // FastEnemy, TankEnemy, etc. added here once they're wired into wave generation.
    }
}