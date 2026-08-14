namespace TestGame001
{
    // Builds a Wave's contents from a wave number. This is the one place wave difficulty
    // scaling lives - tune GameConstants.BaseWaveSize/WaveSizeGrowthPerWave to change pacing.
    public static class WaveGenerator
    {
        public static Wave Generate(int waveNumber)
        {
            var wave = new Wave();

            int enemyCount = GameConstants.BaseWaveSize + (waveNumber - 1) * GameConstants.WaveSizeGrowthPerWave;

            for (int i = 0; i < enemyCount; i++)
            {
                wave.EnemySpawns.Add(EnemyType.Basic);
            }

            return wave;
        }
    }
}