using System.Collections.Generic;

public class Cell: Position
{
    public readonly List<Cell> Neighbors;

    public Cell(int x, int y): base(x,y)
    {
        Neighbors = new List<Cell>();
    }

    public override string ToString()
    {
        return $"({x.ToString()},{y.ToString()})";
    }
}
