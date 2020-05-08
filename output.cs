using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.ComponentModel;


 // LastEdited: 08/05/2020 12:01 



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
    private readonly GameState gameState;

    public GameAI(Map map, GameState gameState)
    {
        this.map = map;
        this.gameState = gameState;
    }

    public void ComputeMoves()
    {
        var myPacs = gameState.myPacs.ToList();
        var pellets = gameState.visiblePellets.Values.ToList();

        var random = new Random();
        foreach (var pac in myPacs)
        {
            if (GameState.CurrentMoves.TryGetValue(pac.pacId, out var existingMove))
            {
                // Check pellet is still here
                if (gameState.visiblePellets.ContainsKey(existingMove.Coord) == false)
                {
                    //assign a new move
                    AssignMoveToPac(pellets, random, pac);
                }
            }
            else
            {
                //Assign a move to this pac
                AssignMoveToPac(pellets, random, pac);
            }

        }
    }

    private static void AssignMoveToPac(List<Pellet> pellets, Random random, Pac pac)
    {
        var randomPellet = pellets[random.Next(pellets.Count)];
        var move = new Move(pac.pacId, randomPellet.x, randomPellet.y);

        GameState.CurrentMoves[pac.pacId] = move;

        pellets.Remove(randomPellet);
    }
}

public class GameState
{
    public static Dictionary<int, Move> CurrentMoves = 
        new Dictionary<int, Move>();

    public static string GetMoves()
    {
        return string.Join('|',
            CurrentMoves.Values.Select(m => m.ToString()));
           
    }

    public int myScore, opponentScore;

    public readonly List<Pac> myPacs;
    public readonly List<Pac> enemyPacs;

    public readonly Dictionary<(int,int),Pellet> visiblePellets;

    public GameState(int myScore, int opponentScore, List<Pac> visiblePacs, List<Pellet> visiblePellets)
    {
        this.myScore = myScore;
        this.opponentScore = opponentScore;

        this.myPacs = new List<Pac>(visiblePacs.Count);
        this.enemyPacs = new List<Pac>(visiblePacs.Count);

        foreach(var pac in visiblePacs)
        {
            if(pac.mine)
            {
                myPacs.Add(pac);
            }
            else
            {
                enemyPacs.Add(pac);
            }
        }

        this.visiblePellets = visiblePellets.ToDictionary(
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

            var gameState = new GameState(myScore, opponentScore, pacs, pellets);

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            var gameAI = new GameAI(map, gameState);
            gameAI.ComputeMoves();

            Console.WriteLine(GameState.GetMoves()); // MOVE <pacId> <x> <y>

        }
    }
}