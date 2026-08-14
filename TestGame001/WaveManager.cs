using System;
using System.Collections.Generic;

namespace TestGame001
{
    // Owns wave progression: works through the current Wave's spawn list with randomized gaps
    // between each enemy, then pauses before generating and starting the next wave.
    public class WaveManager
    {
        private readonly Random random = new Random();
        private readonly Dictionary<int, WaveProgress> progressByWave = new Dictionary<int, WaveProgress>();
        private Wave currentWave;
        private int nextSpawnIndex;
        private float timeUntilNextSpawn;
        private float timeUntilNextWave;
        private bool waveInProgress;

        public int CurrentWaveNumber { get; private set; } = 0;
        private class WaveProgress
        {
            public int RemainingToResolve;
            public bool AnyEscaped;
        }
        public struct WaveClearResult
        {
            public int WaveNumber;
            public int BonusGold;
        }

        public WaveManager()
        {
            timeUntilNextWave = GameConstants.InitialWaveDelaySeconds;
        }

        // Call once per frame (only while the game isn't paused). Returns the EnemyType to spawn
        // this frame, or null if nothing should spawn.
        public EnemyType? Update(float deltaSeconds)
        {
            if (!waveInProgress)
            {
                timeUntilNextWave -= deltaSeconds;
                if (timeUntilNextWave <= 0f)
                {
                    StartNextWave();
                }
                return null;
            }

            timeUntilNextSpawn -= deltaSeconds;
            if (timeUntilNextSpawn > 0f || nextSpawnIndex >= currentWave.EnemySpawns.Count)
            {
                return null;
            }

            EnemyType typeToSpawn = currentWave.EnemySpawns[nextSpawnIndex];
            nextSpawnIndex++;
            timeUntilNextSpawn = GetRandomSpawnDelay();

            if (nextSpawnIndex >= currentWave.EnemySpawns.Count)
            {
                waveInProgress = false;
                timeUntilNextWave = GameConstants.InterWaveDelaySeconds;
            }

            return typeToSpawn;
        }
        // Called by Game1 whenever an enemy is removed (killed or reached the exit), so this
        // wave's tally can be updated. Returns a clear result if this removal was the last
        // outstanding enemy from its wave and none of them escaped - null otherwise.
        public WaveClearResult? ReportEnemyRemoved(int waveNumber, bool escaped)
        {
            System.Diagnostics.Debug.WriteLine($"ReportEnemyRemoved called - wave {waveNumber}, escaped {escaped}");

            if (!progressByWave.TryGetValue(waveNumber, out WaveProgress progress))
            {
                System.Diagnostics.Debug.WriteLine($"No progress entry found for wave {waveNumber}!");
                return null;
            }

            progress.RemainingToResolve--;
            if (escaped)
            {
                progress.AnyEscaped = true;
            }

            System.Diagnostics.Debug.WriteLine($"Wave {waveNumber}: RemainingToResolve = {progress.RemainingToResolve}, AnyEscaped = {progress.AnyEscaped}");

            if (progress.RemainingToResolve > 0)
            {
                return null; // wave not fully resolved yet
            }

            progressByWave.Remove(waveNumber);

            System.Diagnostics.Debug.WriteLine($"Wave {waveNumber} resolved. AnyEscaped: {progress.AnyEscaped}");

            if (progress.AnyEscaped)
            {
                return null; // resolved, but not a clean clear - no bonus
            }

            return new WaveClearResult
            {
                WaveNumber = waveNumber,
                BonusGold = GameConstants.WaveClearBonusGold
            };
        }
        private void StartNextWave()
        {
            CurrentWaveNumber++;

            if (!WaveOverrides.TryGet(CurrentWaveNumber, out currentWave))
            {
                currentWave = WaveGenerator.Generate(CurrentWaveNumber);
            }

            nextSpawnIndex = 0;
            timeUntilNextSpawn = 0f;
            waveInProgress = true;

            progressByWave[CurrentWaveNumber] = new WaveProgress
            {
                RemainingToResolve = currentWave.EnemySpawns.Count
            };
        }

        private float GetRandomSpawnDelay()
        {
            return GameConstants.MinSpawnDelaySeconds +
                (float)random.NextDouble() * (GameConstants.MaxSpawnDelaySeconds - GameConstants.MinSpawnDelaySeconds);
        }
    }
}