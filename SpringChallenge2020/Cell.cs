using System.Collections.Generic;

public class Cell
{
    public readonly int x, y;

    public readonly List<Cell> Neighbors;

    public Cell(int x, int y)
    {
        this.x = x;
        this.y = y;

        Neighbors = new List<Cell>();
    }

    public override string ToString()
    {
        return $"({x.ToString()},{y.ToString()})";
    }
}
