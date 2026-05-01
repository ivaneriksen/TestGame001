using System;
using Microsoft.Xna.Framework;

public class Tower
{
    public Vector2 Position;
    public static float DefaultRange = 150f;
    public float Range = 150f;
    public int Cost = 50;

    public Tower(Vector2 position)
    {
        Position = position;
    }

    
}
