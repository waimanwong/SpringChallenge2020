using System.Collections.Generic;

public class Cell: Position
{
    public readonly Dictionary<Direction, Cell> Neighbors;

    public static Dictionary<(int, int), Cell> CellWithSuperPellets = new Dictionary<(int, int), Cell>();

    private int pelletValue;

    public int PelletValue 
    { 
        get
        {
            return this.pelletValue;
        }
        set
        {
            var coord = this.Coord;
            if(pelletValue == 10)
            {
                CellWithSuperPellets[coord] = this;
            }
            else if(pelletValue == 0)
            {
                if(CellWithSuperPellets.ContainsKey(coord))
                {
                    CellWithSuperPellets.Remove(coord);
                }
            }

            this.pelletValue = value;
        } 
    }

    public Cell(int x, int y): base(x,y)
    {
        Neighbors = new Dictionary<Direction, Cell>();
        this.pelletValue = 1;
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
