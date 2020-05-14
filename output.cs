using System.Runtime.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Net.Security;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Diagnostics;


 // LastEdited: 14/05/2020 23:33 



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

public class GameAI
{
    public void ComputeActions()
    {
        var myPacs = GameState.myPacs.ToList();
        var random = new Random();

        var myZones = RunSimulation(
            GameState.myPacs.Values.ToList(), 
            GameState.enemyPacs.Values.ToList());

        var pacsWithSpeed = new List<Pac>();

        var choosenDirection = new Dictionary<int, Direction>();

        foreach (var kvp in myPacs)
        {
            var pacId = kvp.Key;
            var pac = kvp.Value;

            if (pac.abilityCooldown == 0)
            {
                pac.ActivateSpeed();
                continue;
            }
            
            var bestZone = myZones
                .Where(kvp => kvp.Value.pacId == pacId)
                .Select(kvp => kvp.Value)
                .OrderByDescending(zone => zone.Score)
                .First();

            pac.Move(bestZone.direction);

            if( pac.speedTurnsLeft > 0)
            {
                choosenDirection[pacId] = bestZone.direction;
                pacsWithSpeed.Add(pac);
            }
        }

        if(pacsWithSpeed.Count > 0)
        {
            var secondTurnZones = RunSimulation(
                GameState.myPacs.Values.ToList(),
                GameState.enemyPacs.Values.ToList());

            foreach(var pac in pacsWithSpeed)
            {
                var previousChoosenDirection = choosenDirection[pac.pacId];
                var oppositeDirection = GetOppositeDirection(previousChoosenDirection);

                var zonesOfPac = secondTurnZones
                    .Where(kvp => kvp.Value.pacId == pac.pacId)
                    .ToList();

                if (zonesOfPac.Count == 1)
                {
                    if(zonesOfPac.Single().Value.direction != oppositeDirection)
                    {
                        pac.Move(zonesOfPac.Single().Value.direction);
                    }
                    else
                    {
                        // do not move the second round
                    }
                }
                else
                {
                    var bestZone = zonesOfPac
                        //Avoid going back to 
                        .Where(kvp => kvp.Value.direction != oppositeDirection)
                        .Select(kvp => kvp.Value)
                        .OrderByDescending(zone => zone.Score)
                        .First();

                    pac.Move(bestZone.direction);
                }
            }
        }
    }

    private static Direction GetOppositeDirection(Direction direction)
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

    public Dictionary<Guid,Zone> RunSimulation(List<Pac> myPacs, List<Pac> enemyPacs)
    {
        foreach(var kvp in Map.Cells)
        {
            var cell = kvp.Value;
            cell.RemoveColor();
        }

        var myZones = InitializeZones(myPacs);
        var enemyZones = InitializeZones(enemyPacs);

        var turn = 1;
        var newFrontier = new List<(int, int)>();

        while (turn <= 30)
        {
            newFrontier.Clear();

            foreach (var zone in myZones.Values)
            {
                newFrontier.AddRange(zone.Expand());
            }

            foreach(var zone in enemyZones.Values)
            {
                newFrontier.AddRange(zone.Expand());
            }

            foreach(var coord in newFrontier.ToHashSet())
            {
                if(Map.Cells[coord].tempFloodZoneIds.Count == 1)
                {
                    var zoneId = Map.Cells[coord].tempFloodZoneIds.Single();
                    Map.Cells[coord].OwnedByZone = zoneId;

                    if(myZones.TryGetValue(zoneId, out var myZone))
                    {
                        myZone.AddCoord(coord, turn);
                    }

                    if (enemyZones.TryGetValue(zoneId, out var enemyZone))
                    {
                        enemyZone.AddCoord(coord, turn);
                    }

                }
                else
                {
                    Map.Cells[coord].OwnedByZone = Zone.NeutralZoneId;
                }
            }

            turn++;
        }

        DebugZones(myZones);

        return myZones;
    }

    private void DebugZones(Dictionary<Guid, Zone> zones)
    {
        foreach (var kvp in zones)
        {
            kvp.Value.Debug();
        }

        //var row = new StringBuilder();

        //for (int y = 0; y < Map.Height; y++)
        //{
        //    row.Clear();
        //    for (int x = 0; x < Map.Width; x++)
        //    {
        //        var coord = (x, y);
        //        if (Map.Cells.TryGetValue(coord, out var cell))
        //        {
        //            if (cell.OwnedByZone.HasValue)
        //            {
        //                if (zones.TryGetValue(cell.OwnedByZone.Value, out var zone))
        //                {
        //                    row.Append($"{zone.pacId}");
        //                }
        //                else
        //                {
        //                    row.Append('.');
        //                }
        //            }
        //            else
        //            {
        //                row.Append(' ');
        //            }
        //        }
        //        else
        //            row.Append('#');
        //    }
        //    Player.Debug(row.ToString());
        //}
    }

    private Dictionary<Guid, Zone> InitializeZones(List<Pac> myPacs)
    {
        var zones = new Dictionary<Guid, Zone>();

        foreach (var pac in myPacs)
        {
            var pacCell = Map.Cells[pac.Coord];
            var neighbors = pacCell.Neighbors;

            foreach (var neighbor in neighbors)
            {
                var direction = neighbor.Key;
                var start = neighbor.Value.Coord;

                var zone = new Zone(pac, direction, start);
                zones.Add(zone.Id, zone);
            }

        }

        return zones;
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
        }

        GameState.enemyPacs = enemyVisiblePacsById
            .Where(kvp => kvp.Value.IsDead == false)
            .ToDictionary(keySelector: kvp => kvp.Key, elementSelector: kvp => kvp.Value);
        GameState.visiblePellets = visiblePellets;

    }

    private static void RemoveMyDeadPacman(Dictionary<int, Pac> myVisiblePacsById)
    {
        foreach(var kvp in myVisiblePacsById)
        {
            var pac = kvp.Value;
            
            if(pac.IsDead)
            {
                var deadPacId = pac.pacId;
                if(myPacs.ContainsKey(deadPacId))
                {
                    myPacs.Remove(deadPacId);
                }
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

public class Pac: Position
{
    public readonly int pacId;
    public readonly bool mine;
  
    public string typeId; // unused in wood leagues
    public int speedTurnsLeft; // unused in wood leagues

    public int abilityCooldown; // unused in wood leagues

    private Move currentMove;
    private bool activateSpeed = false;

    private string newType;

    public Direction? bestDirection;

    private bool isBlocked = false;

    public Pac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown): base(x,y)
    {
        this.pacId = pacId;
        this.mine = mine;
        this.typeId = typeId;
        this.speedTurnsLeft = speedTurnsLeft;
        this.abilityCooldown = abilityCooldown;
    }

    public bool IsDead => typeId == "DEAD";

    public void UpdateState(Pac visiblePac)
    {
        CheckIfBlocked(visiblePac);

        this.x = visiblePac.x;
        this.y = visiblePac.y;

        this.typeId = visiblePac.typeId;
        this.speedTurnsLeft = visiblePac.speedTurnsLeft;
        this.abilityCooldown = visiblePac.abilityCooldown;

        this.activateSpeed = false;
        this.newType = string.Empty;

    }

    private void CheckIfBlocked(Pac visiblePac)
    {
        var lastActionIsMove = (this.activateSpeed == false);
       isBlocked = lastActionIsMove && this.x == visiblePac.x && this.y == visiblePac.y; 
    }

    public void Move(Direction direction)
    {
        var cell = Map.Cells[this.Coord].Neighbors[direction];

        this.currentMove = new Move(this.pacId, cell.x, cell.y);
        
        this.x = cell.x;
        this.y = cell.y;

    }

    public void SwitchToType(string newType)
    {
        this.newType = newType;
    }

    public void ActivateSpeed()
    {
        this.activateSpeed = true;
    }

    public string GetCommand()
    {
        if(string.IsNullOrEmpty(this.newType) == false)
        {
            return $"SWITCH {this.pacId.ToString()} {this.newType}";
        }

        if( this.activateSpeed )
        {
            return $"SPEED {this.pacId.ToString()}";
        }

        return $"{this.currentMove.ToString()}";
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
        //Console.Error.WriteLine(message);
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

                    Player.Debug(pacState);
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

public class Zone
{
    public static Guid NeutralZoneId = new Guid("{B7BAD255-8A34-4870-BA36-BF813F896BA6}");

    public Guid Id { get; }

    public int pacId { get; }

    public Direction direction { get; }

    private List<(int, int)> Frontier = new List<(int, int)>();

    /// <summary>
    /// Coords in the zone
    /// </summary>
    private HashSet<(int, int)> Coords = new HashSet<(int, int)>();

    public Zone(Pac pac, Direction direction, (int, int) start)
    {
        this.Id = Guid.NewGuid();
        this.pacId = pac.pacId;
        this.direction = direction;

        Map.Cells[pac.Coord].OwnedByZone = NeutralZoneId;
        Map.Cells[start].OwnedByZone = this.Id;

        AddCoord(start, 0);

        Frontier.Add(start);
    }

    public List<(int, int)> Expand()
    {
        var newFrontier = new List<(int, int)>();

        foreach (var currentPos in Frontier)
        {
            foreach (var neighbor in Map.Cells[currentPos].Neighbors)
            {
                var neighborCell = neighbor.Value;
                if (neighborCell.OwnedByZone.HasValue == false)
                {
                    neighborCell.Color(this.Id);
                    newFrontier.Add(neighborCell.Coord);
                }
            }
        }

        this.Frontier = newFrontier;

        return newFrontier;
    }

    private Dictionary<int, List<(int, int)>> frontiers = new Dictionary<int, List<(int, int)>>();

    public void AddCoord((int, int) coord, int turn)
    {
        Coords.Add(coord);

        if (frontiers.ContainsKey(turn) == false)
            frontiers[turn] = new List<(int, int)>();

        frontiers[turn].Add(coord);
    }

    public double Score 
    { 
        get 
        {
            double score = 0;
            foreach(var kvp in frontiers)
            {
                var maxScoreAtFrontier = kvp.Value
                    .Select(coord => Map.Cells[coord].PelletValue)
                    .OrderByDescending(v => v)
                    .First();
                var turn = kvp.Key;

                score += maxScoreAtFrontier * Math.Pow(10, -turn);
            }

            return score;
        } 
    }
        

    public void Debug()
    {
        Player.Debug($"Zone {pacId} {direction.ToString()} score = {this.Score}");

        //var row = new StringBuilder();

        //for (int y = 0; y < Map.Height; y++)
        //{
        //    row.Clear();
        //    for (int x = 0; x < Map.Width; x++)
        //    {
        //        var coord = (x, y);
        //        if (Map.Cells.TryGetValue(coord, out var cell))
        //        {
        //            if(cell.OwnedByZone.HasValue && cell.OwnedByZone.Value == this.Id)
        //            {
        //                row.Append($"{this.pacId}");
        //            }
        //            else
        //            {
        //                row.Append(' ');
        //            }
        //        }
        //        else
        //            row.Append('#');
        //    }
        //    Player.Debug(row.ToString());
        //}
    }
}