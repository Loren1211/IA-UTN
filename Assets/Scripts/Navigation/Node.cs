using UnityEngine;

public enum NodeZone
{
    Corridor,
    HuntingGround,
    SafeZone
}

public class Node : MonoBehaviour
{
    public bool Blocked { get; private set; }
    public int Cost { get; private set; } = 1;
    public Coordinates Coordinates { get; private set; }
    public NodeZone Zone { get; set; } = NodeZone.Corridor;
    public bool IsSafeZone => Zone == NodeZone.SafeZone;

    public void Setup(Coordinates coordinates)
    {
        Coordinates = coordinates;
        Cost = 1;
    }

    public void SetBlocked(bool blocked) => Blocked = blocked;

    public void SetCost(int cost) => Cost = Mathf.Clamp(cost, 1, 5);
}

public struct Coordinates
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public Coordinates Up => new(X, Y + 1);
    public Coordinates Down => new(X, Y - 1);
    public Coordinates Right => new(X + 1, Y);
    public Coordinates Left => new(X - 1, Y);

    public Coordinates(int x, int y)
    {
        X = x;
        Y = y;
    }
}
