using System.Collections.Generic;

public class Cell: Position
{
    public readonly Dictionary<Direction, Cell> Neighbors;

    public Cell(int x, int y): base(x,y)
    {
        Neighbors = new Dictionary<Direction, Cell>();
    }

    public override string ToString()
    {
        return $"({x.ToString()},{y.ToString()})";
    }
}

public enum Direction
{
    North, 
    South, 
    East, 
    West
}
