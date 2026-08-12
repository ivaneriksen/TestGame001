namespace TestGame001
{
    // Determines which enemy a tower prioritizes when multiple are in range at once.
    public enum TargetingMode
    {
        ClosestToTower,  // targets whichever enemy is nearest to this tower
        MostHealth,      // targets whichever enemy currently has the most remaining health
        LeastHealth,     // targets whichever enemy currently has the least remaining health
        ClosestToExit    // targets whichever enemy has the least remaining distance to travel along the path
    }
}