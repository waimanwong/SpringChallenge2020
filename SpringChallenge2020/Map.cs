using System.Collections.Generic;
using System.Text;

public class Map
{
    private readonly int _width, _height;
    private readonly Dictionary<(int, int), Cell> _cells;

    public Map(int width, int height, List<string> rows)
    {
        _width = width;
        _height = height;

        _cells = new Dictionary<(int, int), Cell>();
        
        ExtractCells(rows);
        ComputeNeighborCells();
    }

    public List<Cell> GetNeighbors(Position position)
    {
        var currentCell = _cells[(position.x, position.y)];

        return currentCell.Neighbors;
    }

    public void Debug()
    {
        var row = new StringBuilder();

        for (int y = 0; y < _height; y++)
        {
            row.Clear();
            for (int x = 0; x < _width; x++)
            {
                if (_cells.ContainsKey((x, y)))
                    row.Append(' ');
                else
                    row.Append('#');
            }
            Player.Debug(row.ToString());
        }
    }
    private void ExtractCells(List<string> rows)
    {
        for (int y = 0; y < rows.Count; y++)
        {
            var row = rows[y];
            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] == ' ')
                {
                    _cells.Add((x, y), new Cell(x, y));
                }
            }
        }
    }

    private void ComputeNeighborCells()
    {
        foreach(var cell in _cells.Values)
        {
            var cellX = cell.x;
            var cellY = cell.y;
            
            //west
            var westX = (cellX - 1 + _width) % _width;
            if(_cells.TryGetValue((westX, cellY), out Cell westCell))
            {
                cell.Neighbors.Add(westCell);
            }

            //east
            var eastX = (cellX + 1 + _width) % _width;
            if (_cells.TryGetValue((eastX, cellY), out Cell eastCell))
            {
                cell.Neighbors.Add(eastCell);
            }

            //north
            var northY = (cellY - 1);
            if (_cells.TryGetValue((cellX, northY), out Cell northCell))
            {
                cell.Neighbors.Add(northCell);
            }

            //south
            var southY = (cellY + 1);
            if (_cells.TryGetValue((cellX, southY), out Cell southCell))
            {
                cell.Neighbors.Add(southCell);
            }
        }
    }

}
