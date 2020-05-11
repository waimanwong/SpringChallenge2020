using System.Runtime.Serialization;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Text;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;


 // LastEdited: 11/05/2020 22:25 



public class Move 
{
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
    public bool IsCompleted(Pac pac)
    {
        return pac.x == this.x && pac.y == this.y;
    }
    public override string ToString()
    {
        return $"MOVE {pacId.ToString()} {x.ToString()} {y.ToString()}";
    }
}

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

            if (pac.abilityCooldown == 0)
            {
                pac.ActivateSpeed();
                continue;
            }
            
            switch(pac.Behavior)
            {
                case Behavior.CollectPellet:

                    if(pac.HasMove == false)
                    {
                        pac.CollectPellet();
                    }
                    break;

                case Behavior.RandomMove:
                    if (pac.HasMove == false)
                    {
                        pac.RandomMoveTo(random);
                    }
                    break;
            }
                
        }
    }

}

public static class GameState
{
    public readonly static Dictionary<int, Pac> myPacs;
    public static Dictionary<int, Pac> enemyPacs;

    public static int myScore, opponentScore;

    public static Dictionary<(int, int), Pellet> visiblePellets;

    private static int turn;

    static GameState()
    {
        myPacs = new Dictionary<int, Pac>();
    }

    public static bool FirstTurn => turn == 1;

    public static (int,int) GetRandomCellToVisit(Random random)
    {
        var positionsToVisit = Map.Cells.Values.Where(c => c.PelletValue == 1).ToArray();

        var randomIndex = random.Next(positionsToVisit.Length);

        return positionsToVisit[randomIndex].Coord;
    }

    public static void SetState(
        int turn,
        int myScore, int opponentScore, 
        Dictionary<int, Pac> myVisiblePacsById, 
        Dictionary<int, Pac> enemyVisiblePacsById,
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        GameState.turn = turn;
        GameState.myScore = myScore;
        GameState.opponentScore = opponentScore;

        RemoveMyDeadPacman(myVisiblePacsById);

        UpdateVisitedPositions(myVisiblePacsById, enemyVisiblePacsById, visiblePellets);

        foreach (var kvp in myVisiblePacsById)
        {
            var pacId = kvp.Key;
            var visiblePac = kvp.Value;

            if (myPacs.ContainsKey(pacId) == false)
            {
                myPacs[pacId] = visiblePac;
            }
            else
            {
                myPacs[pacId].UpdateState(visiblePac);
            }

            var newBehavior = myPacs[pacId].ComputeBehavior(myVisiblePacsById, enemyVisiblePacsById, visiblePellets);
        }

        GameState.enemyPacs = enemyVisiblePacsById;
        GameState.visiblePellets = visiblePellets;

    }

    private static void RemoveMyDeadPacman(Dictionary<int, Pac> myVisiblePacsById)
    {
        foreach (var kvp in myPacs)
        {
            var myPacId = kvp.Key;
            if (myVisiblePacsById.ContainsKey(myPacId) == false)
            {
                myPacs.Remove(myPacId);
            }
        }
    }

    private static void UpdateVisitedPositions(
        Dictionary<int, Pac> myVisiblePacsById,
        Dictionary<int, Pac> enemyVisiblePacsById,
        Dictionary<(int, int), Pellet> visiblePelletsByCoord)
    {
        #region update high value pellets
        //update for high value pellets
        var superPelletCoords = visiblePelletsByCoord
            .Where(kvp => kvp.Value.IsSuperPellet)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var superPelletCoord in superPelletCoords)
        {
            Map.Cells[superPelletCoord].PelletValue = 10;
        }

        //update picked high value pellets
        var knownSuperPelletCoords = Cell.CellWithSuperPellets.Keys.ToList();
        foreach(var coord in knownSuperPelletCoords)
        {
            if( visiblePelletsByCoord.ContainsKey(coord) ==  false)
            {
                //pellet got picked
                Map.Cells[coord].PelletValue = 0;
            }
        }
        #endregion

        var enemyKnownPositions = enemyVisiblePacsById.Values.Select(p => p.Coord).ToHashSet();
        var visiblePellets = visiblePelletsByCoord.Keys.ToHashSet();

        foreach (var kvp in myVisiblePacsById)
        {
            var visiblePac = kvp.Value;
            var pacCoord = (visiblePac.x, visiblePac.y);

            //pac position is visited
            Map.Cells[pacCoord].PelletValue = 0;

            //Update based on the vision of the pac
            foreach (var direction in new[] { Direction.East, Direction.North, Direction.South, Direction.West })
            {
                var currentCell = Map.Cells[(visiblePac.x, visiblePac.y)];
                var processedPositions = new HashSet<(int, int)>();

                processedPositions.Add(currentCell.Coord);

                while (currentCell.Neighbors.TryGetValue(direction, out var nextCell))
                {
                    if (processedPositions.Contains(nextCell.Coord))
                        break;

                    currentCell = nextCell;

                    var thereIsAnEnemyAtCurrentCell = enemyKnownPositions.Contains(currentCell.Coord);
                    var thereIsNotPelletAtCurrentCell = visiblePellets.Contains(currentCell.Coord) == false;

                    if(thereIsAnEnemyAtCurrentCell || thereIsNotPelletAtCurrentCell)
                    {
                        //place is visited
                        Map.Cells[currentCell.Coord].PelletValue = 0;
                    }
                    
                }
            }
        }

        foreach(var enemyKnownPosition in enemyKnownPositions)
        {
            Map.Cells[enemyKnownPosition].PelletValue = 0;
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
                    var cellValue = Map.Cells[coord].PelletValue;
                    switch(cellValue)
                    {
                        case 10:
                            row.Append('o');
                            break;

                        case 1:
                            row.Append('.');
                            break;
                        
                        case 0:
                            if (myPacs.TryGetValue(coord, out var myPac))
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
                            break;

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
        }
    }

}


public enum Behavior
{
    RandomMove,
    CollectPellet
}

public class Pac: Position
{
    public readonly int pacId;
    public readonly bool mine;
  
    public string typeId; // unused in wood leagues
    public int speedTurnsLeft; // unused in wood leagues

    public int abilityCooldown; // unused in wood leagues

    private Move currentMove;
    private bool activateSpeed = false;

    private string recommendedType;

    public Direction? bestDirection;

    private Behavior _behavior;
    private bool isBlocked = false;


    public Behavior Behavior { 
        get { return _behavior; }
        private set
        {
            if( this._behavior != value)
            {
                //Changing behavior => cancel current action
                currentMove = null;
            }
            this._behavior = value;
        } 
    }

    public Pac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown): base(x,y)
    {
        this.pacId = pacId;
        this.mine = mine;
        this.typeId = typeId;
        this.speedTurnsLeft = speedTurnsLeft;
        this.abilityCooldown = abilityCooldown;

        this.Behavior = Behavior.RandomMove;

    }

    public void UpdateState(Pac visiblePac)
    {
        CheckIfBlocked(visiblePac);

        this.x = visiblePac.x;
        this.y = visiblePac.y;

        this.typeId = visiblePac.typeId;
        this.speedTurnsLeft = visiblePac.speedTurnsLeft;
        this.abilityCooldown = visiblePac.abilityCooldown;

        this.activateSpeed = false;
        this.recommendedType = string.Empty;

        CheckCurrentMoveCompletion();
    }

    private void CheckIfBlocked(Pac visiblePac)
    {
        var lastActionIsMove = (this.activateSpeed == false);
       isBlocked = lastActionIsMove && this.x == visiblePac.x && this.y == visiblePac.y; 
    }

    private void CheckCurrentMoveCompletion()
    {
        if (this.currentMove != null  && this.currentMove.IsCompleted(this))
        {
            this.currentMove = null;
        }

        if(this.isBlocked)
        {
            //Cancel current move
            this.currentMove = null;
        }
    }

    public Behavior ComputeBehavior(
        Dictionary<int, Pac> myVisiblePacsById,
        Dictionary<int, Pac> enemyVisiblePacsbyId, 
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        if (isBlocked)
        {
            Player.Debug($"{pacId} is blocked.");
            this.Behavior = Behavior.RandomMove;
            return this.Behavior;
        }
        
        SetBestDirection(myVisiblePacsById, visiblePellets);

        if (this.bestDirection == null)
        {
            this.Behavior = Behavior.RandomMove;
        }
        else
        {
            this.Behavior = Behavior.CollectPellet;
        }

        return this.Behavior;
    }

    private void SetBestDirection(
        Dictionary<int, Pac> myVisiblePacsById, 
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        bestDirection = null;

        double bestScore = 0;

        var myVisiblePacs = myVisiblePacsById.Values
            .ToDictionary(keySelector: pac => pac.Coord, elementSelector: pac => pac);

        foreach (var direction in new[] { Direction.East, Direction.North, Direction.South, Direction.West })
        {
            
            var distance = 0;
            var currentCell = Map.Cells[(this.x, this.y)];
            double score = 0;
            var visitedPosition = new HashSet<(int, int)>();

            visitedPosition.Add(currentCell.Coord);

            while (currentCell.Neighbors.TryGetValue(direction, out var nextCell))
            {
                if (visitedPosition.Contains(nextCell.Coord))
                    break;

                distance += 1;
                currentCell = nextCell;

                visitedPosition.Add(currentCell.Coord);

                if (myVisiblePacs.TryGetValue(currentCell.Coord, out var myBlockingPac))
                {
                    //TO DO: more complicated logic (if enemy ...)
                    //Block in one way
                    if (myBlockingPac.x < this.x || myBlockingPac.y < this.y)
                    {
                        break;
                    }
                }

                if (visiblePellets.TryGetValue(currentCell.Coord, out var visiblePellet))
                {
                    score += (visiblePellet.value * Math.Pow(10, -distance));
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = direction;
            }
        }
    }

    public bool HasMove => currentMove != null;

    public void CollectPellet()
    {
        var choosenDirection = this.bestDirection.Value;
        var cell = Map.Cells[this.Coord].Neighbors[choosenDirection];
        
        this.currentMove = new Move(this.pacId, cell.x, cell.y);

        if(speedTurnsLeft > 0)
        {
            //Can move one cell further
            this.x = cell.x;
            this.y = cell.y;

            if(Map.Cells[this.Coord].Neighbors.TryGetValue(choosenDirection, out var secondCell))
            {
                //keep going in the same direction
                this.currentMove = new Move(this.pacId, secondCell.x, secondCell.y);
                return;
            }

            this.Behavior = Behavior.RandomMove;

            var possibleNeighbors = Map.Cells[this.Coord].Neighbors.Select(kvp => kvp.Value.Coord).ToHashSet();
                
            // priority to not visited
            foreach (var possibleNeighbor in possibleNeighbors)
            {
                if(Map.Cells[possibleNeighbor].PelletValue > 0)
                {
                    //Second move here
                    this.currentMove = new Move(this.pacId, possibleNeighbor.Item1, possibleNeighbor.Item2);
                    return;
                }
            }

            //Otherwise...
            var defaultChoice = possibleNeighbors.First();
            this.currentMove = new Move(this.pacId, defaultChoice.Item1, defaultChoice.Item2);
            return;
        }

    }

    public void RandomMoveTo(Random random)
    {
        var (targetX, targetY) = GameState.GetRandomCellToVisit(random);

        this.currentMove = new Move(this.pacId, targetX, targetY);
    }

    public void SwitchToType(string recommendedType)
    {
        this.recommendedType = recommendedType;
    }

    public void ActivateSpeed()
    {
        this.activateSpeed = true;
    }

    public string GetCommand()
    {
        if(string.IsNullOrEmpty(this.recommendedType) == false)
        {
            return $"SWITCH {this.pacId.ToString()} {this.recommendedType}";
        }

        if( this.activateSpeed )
        {
            return $"SPEED {this.pacId.ToString()} {this.Behavior.ToString()}";
        }

        var message = $"{this.Behavior.ToString().First()} {this.currentMove.x} {this.currentMove.y}";

        return $"{this.currentMove.ToString()} {message}";
    }


}
public class Pellet: Position
{
    public readonly int value;

    public bool IsSuperPellet => value == 10;

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

        int turn = 0;

        // game loop
        while (true)
        {
            turn++;

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

                Player.Debug(pacState);

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

            if(turn == 1)
            {
                //Map is symetric so we know where the enemies are
                foreach(var kvp in myPacs)
                {
                    var myPac = kvp.Value;
                    var myPacId = kvp.Key;

                    if (enemyPacs.ContainsKey(myPacId) == false)
                    {
                        var symetricPac = new Pac(myPac.pacId, false, width - myPac.x - 1, myPac.y, myPac.typeId, myPac.speedTurnsLeft, myPac.abilityCooldown);
                        enemyPacs.Add(myPacId, symetricPac);
                    }
                }
                
            }

            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            var pellets = new Dictionary<(int,int), Pellet>(visiblePelletCount);
            for (int i = 0; i < visiblePelletCount; i++)
            {
                var pellet = Console.ReadLine();
                
                inputs = pellet.Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth

                pellets.Add((x,y), new Pellet(x, y, value));
            }

            GameState.SetState(turn, myScore, opponentScore, myPacs, enemyPacs, pellets);

            GameState.Debug();

            var gameAI = new GameAI();
            gameAI.ComputeActions();

            var actions = string.Join('|', GameState.myPacs.Values.Select(pac => pac.GetCommand()));

            Player.Debug($"{watch.ElapsedMilliseconds.ToString()} ms");

            Console.WriteLine($"{actions}"); // MOVE <pacId> <x> <y>

        }
    }
}

public static class TypeAnalyzer
{
    public const string ROCK = "ROCK";
    public const string PAPER = "PAPER";
    public const string SCISSORS = "SCISSORS";

    /// <summary>
    /// return signednumber: myType - enemyType
    /// </summary>
    /// <param name="myType"></param>
    /// <param name="enemyType"></param>
    /// <returns></returns>
    public static int Compare(string myType, string enemyType)
    {   
        switch (myType)
        {
            case ROCK:
                switch (enemyType)
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