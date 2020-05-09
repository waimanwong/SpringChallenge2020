using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.ComponentModel;


 // LastEdited: 09/05/2020 23:35 



public abstract class Action 
{
    public int pacId;

    public Action(int pacId)
    {
        this.pacId = pacId;
    }
} 

public class Speed : Action
{
    public Speed(int pacId): base(pacId)
    {  
    }

    public override string ToString()
    {
        return $"SPEED {pacId.ToString()}";
    }
}

public class Switch : Action
{
    public string pacType;

    public Switch(int pacId, string pacType): base(pacId)
    {
        this.pacType = pacType;
    }
    public override string ToString()
    {
        return $"SWITCH {pacId.ToString()} {pacType.ToString()}";
    }
}

public class Move : Action
{
    public int x;
    public int y;

    public Move(int pacId, int x, int y): base(pacId)
    {
        this.x = x;
        this.y = y;
    }

    public (int, int) Coord => (x, y);

    public override string ToString()
    {
        return $"MOVE {pacId.ToString()} {x.ToString()} {y.ToString()}";
    }
}

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
    public void ComputeActions()
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
        var (x,y) = GameState.GetRandomCellToVisit(random);

        var move = new Move(pac.pacId, x, y);

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
    public static HashSet<(int, int)> RemainingCellsToVisit;

    public static string GetMoves()
    {
        return string.Join('|',
            CurrentMoves.Values.Select(m => m.ToString()));
           
    }

    static GameState()
    {
        myPacs = new Dictionary<int, Pac>();
    }

    public static void InitializeRemainingCellsToVisit()
    {
        RemainingCellsToVisit = Map.Cells.Keys.ToHashSet();
    }

    public static (int,int) GetRandomCellToVisit(Random random)
    {
        var randomIndex = random.Next(RemainingCellsToVisit.Count);

        return RemainingCellsToVisit.ElementAt(randomIndex);
    }

    public static void SetState(int myScore, int opponentScore, 
        Dictionary<int, Pac> myVisiblePacs, Dictionary<int, Pac> enemyVisiblePacs, 
        List<Pellet> visiblePellets)
    {
        GameState.myScore = myScore;
        GameState.opponentScore = opponentScore;

        var newEnemyPacs = new Dictionary<int, Pac>();

        GameState.MyPacsDidNotMove = new HashSet<int>();

        foreach (var kvp in myVisiblePacs)
        {
            var pacId = kvp.Key;
            var visiblePac = kvp.Value;

            if (myPacs.TryGetValue(pacId, out var currentPac))
            {
                if (currentPac.x == visiblePac.x && currentPac.y == visiblePac.y)
                {
                    GameState.MyPacsDidNotMove.Add(pacId);
                }
            }

            HasVisitedPosition(visiblePac);
        }

        GameState.myPacs = myVisiblePacs;
        GameState.enemyPacs = myVisiblePacs;

        GameState.visiblePellets = visiblePellets.ToDictionary(
            keySelector: pellet => pellet.Coord,
            elementSelector:  pellet => pellet);
    }

    private static void HasVisitedPosition(Pac visiblePac)
    {
        var visitedCoord = (visiblePac.x, visiblePac.y);

        if(RemainingCellsToVisit.Contains(visitedCoord))
        {
            RemainingCellsToVisit.Remove(visitedCoord);
        }
    }

    public static void Debug()
    {
        var row = new StringBuilder();
        var myPacs = GameState.myPacs.Values
            .ToDictionary(keySelector: pac => (pac.x, pac.y), elementSelector: pac => pac);
        var enemyPacs = GameState.enemyPacs.Values
            .ToDictionary(keySelector: pac => (pac.x, pac.y), elementSelector: pac => pac);


        for (int y = 0; y < Map.Height; y++)
        {
            row.Clear();
            for (int x = 0; x < Map.Width; x++)
            {
                var coord = (x, y);
                if (Map.Cells.ContainsKey(coord))
                {
                    if (visiblePellets.TryGetValue(coord, out var pellet))
                    {
                        row.Append(pellet.value == 1 ? 'o' : 'O');
                    }
                    else if(myPacs.TryGetValue(coord, out var myPac))
                    {
                        row.Append(myPac.pacId.ToString());
                    }
                    else if (enemyPacs.TryGetValue(coord, out var enemyPac))
                    {
                        row.Append('!');
                    }
                    else
                    {
                        row.Append(' ');
                    }
                }
                else
                    row.Append('#');
            }
            Player.Debug(row.ToString());
        }
    }

}

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

    public const string ROCK = "ROCK";
    public const string PAPER = "PAPER";
    public const string SCISSORS = "SCISSORS";

    public int Compare(Pac enemyPac)
    {
        var myType = this.typeId;
        var enemyType = enemyPac.typeId;

        switch(myType)
        {
            case ROCK:
                switch(enemyType)
                {
                    case ROCK:
                        return 0;
                    case PAPER:
                        return -1;
                    case SCISSORS:
                        return 1;
                }
                break;

            case "PAPER":
                switch (enemyType)
                {
                    case ROCK:
                        return 1;
                    case PAPER:
                        return 0;
                    case SCISSORS:
                        return -1;
                }
                break;

            case "SCISSORS":
                switch (enemyType)
                {
                    case ROCK:
                        return -1;
                    case PAPER:
                        return 1;
                    case SCISSORS:
                        return 0;
                }
                break;
        }

        throw new Exception();
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

    public int DistanceTo(Position other)
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

            var neighbors = Map.GetNeighbors(currentPosition);

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

        Map.Set(width, height, rows);

        GameState.InitializeRemainingCellsToVisit();
       
        // game loop
        while (true)
        {
            inputs = Console.ReadLine().Split(' ');

            var watch = Stopwatch.StartNew();
            
            int myScore = int.Parse(inputs[0]);
            int opponentScore = int.Parse(inputs[1]);
            
            int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
            
            var enemyPacs = new Dictionary<int, Pac>(visiblePacCount);
            var myPacs = new Dictionary<int, Pac>(visiblePacCount);

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

                var pac = new Pac(pacId, mine, x, y, typeId, speedTurnsLeft, abilityCooldown);
                
                if (mine)
                {
                    myPacs.Add(pac.pacId, pac);
                }
                else
                {
                    enemyPacs.Add(pac.pacId, pac);
                }
            }

            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            var pellets = new List<Pellet>(visiblePelletCount);
            for (int i = 0; i < visiblePelletCount; i++)
            {
                var pellet = Console.ReadLine();
                
                inputs = pellet.Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth

                pellets.Add(new Pellet(x, y, value));
            }

            GameState.SetState(myScore, opponentScore, myPacs, enemyPacs, pellets);

            //GameState.Debug();

            var gameAI = new GameAI();
            gameAI.ComputeActions();

            var actions = string.Join('|', GameState.CurrentMoves.Select(m => m.Value.ToString()));

            Console.WriteLine($"{actions} {watch.ElapsedMilliseconds.ToString()}"); // MOVE <pacId> <x> <y>

        }
    }
}