using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public static class Map
{
    public static int Width, Height;
    public static Dictionary<(int, int), Cell> Cells;

    public static void Set(int width, int height, List<string> rows)
    {
        Width = width;
        Height = height;

        Cells = new Dictionary<(int, int), Cell>();
        
        ExtractCells(rows);
        var deadEndCells = ComputeNeighborCells();
        SetCoefficient(deadEndCells);
    }

    private static void SetCoefficient(List<Cell> deadEndCells)
    {
        foreach(var deadEnd in deadEndCells)
        {
            var path = new Stack<Cell>();
            path.Push(deadEnd);

            var (currentDirection, currentCell) = deadEnd.Neighbors.Single();
            

            while(currentCell.Neighbors.Count <= 2)
            {
                path.Push(currentCell);

                (currentDirection, currentCell) = currentCell
                    .Neighbors
                    .Where(kvp => kvp.Key != GetOppositeDirection(currentDirection))
                    .Single();

            }

            //Player.Debug(string.Join("->", path.Select(c => c.ToString())));

            while(path.Count > 0)
            {
                var cell = path.Pop();
                cell.Coeff = 0.5;
            }

        }
                
    }

    public static List<Cell> GetNeighbors(Position position)
    {
        var currentCell = Cells[(position.x, position.y)];

        return currentCell.Neighbors.Values.ToList();
    }

    private static void ExtractCells(List<string> rows)
    {
        for (int y = 0; y < rows.Count; y++)
        {
            var row = rows[y];
            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] == ' ')
                {
                    Cells.Add((x, y), new Cell(x, y));
                }
            }
        }
    }

    //Returns cells with one direction (dead end)
    private static List<Cell> ComputeNeighborCells()
    {
        var deadEndCells = new List<Cell>();

        foreach(var cell in Cells.Values)
        {
            var cellX = cell.x;
            var cellY = cell.y;
            
            //west
            var westX = (cellX - 1 + Width) % Width;
            if(Cells.TryGetValue((westX, cellY), out Cell westCell))
            {
                cell.Neighbors.Add(Direction.West, westCell);
            }

            //east
            var eastX = (cellX + 1 + Width) % Width;
            if (Cells.TryGetValue((eastX, cellY), out Cell eastCell))
            {
                cell.Neighbors.Add(Direction.East, eastCell);
            }

            //north
            var northY = (cellY - 1);
            if (Cells.TryGetValue((cellX, northY), out Cell northCell))
            {
                cell.Neighbors.Add(Direction.North, northCell);
            }

            //south
            var southY = (cellY + 1);
            if (Cells.TryGetValue((cellX, southY), out Cell southCell))
            {
                cell.Neighbors.Add(Direction.South, southCell);
            }

            if(cell.Neighbors.Count == 1)
            {
                deadEndCells.Add(cell);
            }
        }

        return deadEndCells;
    }

    public static Direction GetOppositeDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.East:
                return Direction.West;
            case Direction.West:
                return Direction.East;
            case Direction.North:
                return Direction.South;
            case Direction.South:
                return Direction.North;
        }
        throw new NotSupportedException();
    }

}
