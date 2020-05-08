using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        ComputeNeighborCells();
    }

    public static List<Cell> GetNeighbors(Position position)
    {
        var currentCell = Cells[(position.x, position.y)];

        return currentCell.Neighbors;
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

    private static void ComputeNeighborCells()
    {
        foreach(var cell in Cells.Values)
        {
            var cellX = cell.x;
            var cellY = cell.y;
            
            //west
            var westX = (cellX - 1 + Width) % Width;
            if(Cells.TryGetValue((westX, cellY), out Cell westCell))
            {
                cell.Neighbors.Add(westCell);
            }

            //east
            var eastX = (cellX + 1 + Width) % Width;
            if (Cells.TryGetValue((eastX, cellY), out Cell eastCell))
            {
                cell.Neighbors.Add(eastCell);
            }

            //north
            var northY = (cellY - 1);
            if (Cells.TryGetValue((cellX, northY), out Cell northCell))
            {
                cell.Neighbors.Add(northCell);
            }

            //south
            var southY = (cellY + 1);
            if (Cells.TryGetValue((cellX, southY), out Cell southCell))
            {
                cell.Neighbors.Add(southCell);
            }
        }
    }

}
