using System;
using Microsoft.Xna.Framework;

public class Enemy
{
    public Vector2 Position;
    public float Speed = 2f;
    public int Health = 100;
    public int CurrentWaypointIndex = 0;
    public bool IsActive = true;

    public Enemy(Vector2 startPosition)
    {
        Position = startPosition;
    }
}
