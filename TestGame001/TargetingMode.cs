namespace TestGame001
{
    // Determines which enemy a tower prioritizes when multiple are in range at once.
    public enum TargetingMode
    {
        ClosestToTower,
        MostHealth,
        LeastHealth,
        ClosestToExit,
        ClosestToEntrance
    }
}