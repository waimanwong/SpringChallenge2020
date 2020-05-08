using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics;


 // LastEdited: 08/05/2020 13:53 



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

public class GameAI
{
    private readonly Map map;

    public GameAI(Map map)
    {
        this.map = map;
    }

    public void ComputeMoves()
    {
        var myPacs = GameState.myPacs.ToList();
        var random = new Random();

        foreach (var kvp in myPacs)
        {
            var pacId = kvp.Key;
            var pac = kvp.Value;

            if(GameState.MyPacsDidNotMove.Contains(pacId))
            {
                //my pac is stuck, remove current move
                GameState.CurrentMoves.Remove(pacId);
            }

            if (GameState.CurrentMoves.TryGetValue(pacId, out var existingMove))
            {
                // Check pac is at destination
                if (pac.x == existingMove.x && pac.y == existingMove.y)
                {
                    //assign a new move
                    AssignMoveToPac(random, pac);
                }
            }
            else
            {
                //Assign a move to this pac
                AssignMoveToPac(random, pac);
            }
        }

        foreach (var kvp in GameState.CurrentMoves.ToArray())
        {
            if (myPacs.Any(p => p.Key == kvp.Key) == false)
            {
                //pac died: remove move
                GameState.CurrentMoves.Remove(kvp.Key);
            }
        }
    }

    private void AssignMoveToPac(Random random, Pac pac)
    {
        var randomCell = this.map.GetRandomCell(random);

        var move = new Move(pac.pacId, randomCell.x, randomCell.y);

        GameState.CurrentMoves[pac.pacId] = move;
    }
}

public static class GameState
{
    public static Dictionary<int, Move> CurrentMoves = new Dictionary<int, Move>();

    public static Dictionary<int, Pac> myPacs;
    public static Dictionary<int, Pac> enemyPacs;

    public static int myScore, opponentScore;

    public static Dictionary<(int, int), Pellet> visiblePellets;

    public static HashSet<int> MyPacsDidNotMove;

    public static string GetMoves()
    {
        return string.Join('|',
            CurrentMoves.Values.Select(m => m.ToString()));
           
    }

    static GameState()
    {
        myPacs = new Dictionary<int, Pac>();
    }

    public static void SetState(int myScore, int opponentScore, List<Pac> visiblePacs, List<Pellet> visiblePellets)
    {
        GameState.myScore = myScore;
        GameState.opponentScore = opponentScore;

        var newEnemyPacs = new Dictionary<int, Pac>();

        GameState.MyPacsDidNotMove = new HashSet<int>();

        foreach (var newVisiblePac in visiblePacs)
        {
            var pacId = newVisiblePac.pacId;

            if(newVisiblePac.mine)
            {
                if(myPacs.TryGetValue(pacId, out var currentPac))
                {
                    if(currentPac.x == newVisiblePac.x && currentPac.y == newVisiblePac.y)
                    {
                        GameState.MyPacsDidNotMove.Add(pacId);
                    }
                    myPacs[pacId] = newVisiblePac;
                }
                else
                {
                    myPacs[pacId] = newVisiblePac;
                }
            }
            else
            {
                newEnemyPacs[pacId] = newVisiblePac;
            }
        }

        GameState.enemyPacs = newEnemyPacs;

        GameState.visiblePellets = visiblePellets.ToDictionary(
            keySelector: pellet => pellet.Coord,
            elementSelector:  pellet => pellet);
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

    public List<Cell> GetNeighbors(Position position)
    {
        var currentCell = _cells[(position.x, position.y)];

        return currentCell.Neighbors;
    }

    public Cell GetRandomCell(Random random)
    {
        var randomIndex = random.Next(_cells.Count);

        return _cells.Values.ElementAt(randomIndex);
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

public class Move
{
    public bool isMine;
    public int pacId;
    public int x;
    public int y;

    public Move(int pacId, int x, int y)
    {
        this.pacId = pacId;
        this.x = x;
        this.y = y;
    }

    public (int, int) Coord => (x, y);

    public override string ToString()
    {
        return $"MOVE {pacId.ToString()} {x.ToString()} {y.ToString()}";
    }
}
public class Pac: Position
{
    public readonly int pacId;
    public readonly bool mine;
  
    public readonly string typeId; // unused in wood leagues
    public readonly int speedTurnsLeft; // unused in wood leagues
    public readonly int abilityCooldown; // unused in wood leagues

    public Pac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown): base(x,y)
    {
        this.pacId = pacId;
        this.mine = mine;
        this.typeId = typeId;
        this.speedTurnsLeft = speedTurnsLeft;
        this.abilityCooldown = abilityCooldown;
    }

    public void Move(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Pac Clone()
    {
        return new Pac(this.pacId,
                        this.mine,
                        this.x,
                        this.y,
                        this.typeId,
                        this.speedTurnsLeft,
                        this.abilityCooldown);
    }
}
public class Pellet: Position
{
    public readonly int value;

    public Pellet(int x, int y, int value): base(x,y)
    {
        this.value = value;
    }
}

public abstract class Position
{
    public int x; 
    public int y;

    protected Position (int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public (int, int) Coord => (x, y);

    public int DistanceTo(Position other, Map map)
    {
        //BFS
        var visited = new HashSet<(int, int)>();
        var frontier = new Queue<(int,Position)>();
        var distance = 0;

        frontier.Enqueue((0, this));

        while(frontier.Count > 0)
        {
            var (currentDistance, currentPosition) = frontier.Dequeue();

            if(currentPosition.Coord == other.Coord)
            {
                distance = currentDistance;
                break;
            }

            visited.Add(currentPosition.Coord);

            var neighbors = map.GetNeighbors(currentPosition);

            foreach(var neighborCell in neighbors)
            {
                if(visited.Contains(neighborCell.Coord) == false)
                {
                    frontier.Enqueue((currentDistance + 1, neighborCell));
                }
            }
        }

        return distance;
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

        var map = new Map(width, height, rows);
       
        // game loop
        while (true)
        {
            inputs = Console.ReadLine().Split(' ');

            var watch = Stopwatch.StartNew();
            
            int myScore = int.Parse(inputs[0]);
            int opponentScore = int.Parse(inputs[1]);
            
            int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
            var pacs = new List<Pac>(visiblePacCount);
            for (int i = 0; i < visiblePacCount; i++)
            {
                var pacState = Console.ReadLine();

                inputs = pacState.Split(' ');
                int pacId = int.Parse(inputs[0]); // pac number (unique within a team)
                bool mine = inputs[1] != "0"; // true if this pac is yours
                int x = int.Parse(inputs[2]); // position in the grid
                int y = int.Parse(inputs[3]); // position in the grid
                string typeId = inputs[4]; // unused in wood leagues
                int speedTurnsLeft = int.Parse(inputs[5]); // unused in wood leagues
                int abilityCooldown = int.Parse(inputs[6]); // unused in wood leagues

                pacs.Add(new Pac(pacId, mine, x, y, typeId, speedTurnsLeft, abilityCooldown));
            }

            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            var pellets = new List<Pellet>(visiblePelletCount);
            for (int i = 0; i < visiblePelletCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth

                pellets.Add(new Pellet(x, y, value));
            }

            GameState.SetState(myScore, opponentScore, pacs, pellets);

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            var gameAI = new GameAI(map);
            gameAI.ComputeMoves();

            Console.WriteLine($"{GameState.GetMoves()} {watch.ElapsedMilliseconds.ToString()}"); // MOVE <pacId> <x> <y>

        }
    }
}