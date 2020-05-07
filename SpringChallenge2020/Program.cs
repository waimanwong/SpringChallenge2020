using System;
using System.Collections.Generic;
using System.Text;

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

    public void Debug()
    {
        var row = new StringBuilder();
        
        for(int y=0; y < _height; y++ )
        {
            row.Clear();
            for(int x = 0; x < _width; x++)
            {
                if (_cells.ContainsKey((x, y)))
                    row.Append(' ');
                else
                    row.Append('#');
            }
            Player.Debug(row.ToString());
        }
    }
}

/**
 * Grab the pellets as fast as you can!
 **/
public class Player
{
    public static void Debug(string message)
    {
        Console.Error.WriteLine(message);
    }

    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]); // size of the grid
        int height = int.Parse(inputs[1]); // top left corner is (x=0, y=0)

        var rows = new List<string>(height);

        for (int i = 0; i < height; i++)
        {
            string row = Console.ReadLine(); // one line of the grid: space " " is floor, pound "#" is wall
            rows.Add(row);
        }

        Map map = new Map(width, height, rows);
        map.Debug();

        Debug("Game loop");
        // game loop
        while (true)
        {
            var scores = Console.ReadLine();

            Debug(scores);

            inputs = scores.Split(' ');
            int myScore = int.Parse(inputs[0]);
            int opponentScore = int.Parse(inputs[1]);
            int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
            for (int i = 0; i < visiblePacCount; i++)
            {
                var pacState = Console.ReadLine();

                Debug(pacState);

                inputs = pacState.Split(' ');
                int pacId = int.Parse(inputs[0]); // pac number (unique within a team)
                bool mine = inputs[1] != "0"; // true if this pac is yours
                int x = int.Parse(inputs[2]); // position in the grid
                int y = int.Parse(inputs[3]); // position in the grid
                string typeId = inputs[4]; // unused in wood leagues
                int speedTurnsLeft = int.Parse(inputs[5]); // unused in wood leagues
                int abilityCooldown = int.Parse(inputs[6]); // unused in wood leagues
            }
            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            for (int i = 0; i < visiblePelletCount; i++)
            {
                var visiblePellet = Console.ReadLine();

                Debug(visiblePellet);

                inputs = visiblePellet.Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            Console.WriteLine("MOVE 0 15 10"); // MOVE <pacId> <x> <y>

        }
    }
}