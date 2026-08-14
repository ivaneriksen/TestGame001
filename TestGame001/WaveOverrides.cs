using System.Collections.Generic;

namespace TestGame001
{
    // Hand-authored waves that override procedural generation for specific wave numbers -
    // e.g. boss fights. WaveManager checks here first; if a wave number isn't listed, it falls
    // back to WaveGenerator.
    public static class WaveOverrides
    {
        private static readonly Dictionary<int, Wave> Overrides = new Dictionary<int, Wave>
        {
            // Example boss wave at 10 - a single tough enemy instead of the usual batch.
            // Swap EnemyType.Basic for a real boss type once one exists.
            [3] = new Wave { EnemySpawns = { EnemyType.Basic } }
        };

        public static bool TryGet(int waveNumber, out Wave wave)
        {
            return Overrides.TryGetValue(waveNumber, out wave);
        }
    }
}