using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

public class Cell: Position
{
    public readonly Dictionary<Direction, Cell> Neighbors;

    public static Dictionary<(int, int), Cell> CellWithSuperPellets = new Dictionary<(int, int), Cell>();

    private int pelletValue;

    public double Coeff { get; set; } = 1;

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

    public HashSet<Guid> tempFloodZoneIds = new HashSet<Guid>();
    public Guid? OwnedByZone;

    public void Color(Guid guid)
    {
        tempFloodZoneIds.Add(guid);
    }

    public void RemoveColor()
    {
        OwnedByZone = null;
        tempFloodZoneIds.Clear();
    }
}

public enum Direction
{
    North, 
    South, 
    East, 
    West
}
