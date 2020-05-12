using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Security;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

public class GameAI
{
    public void ComputeActions()
    {
        var myPacs = GameState.myPacs.ToList();
        var random = new Random();

        var myZones = RunSimulation(
            GameState.myPacs.Values.ToList(), 
            GameState.enemyPacs.Values.ToList());

        var myPacsWithSpeed = new List<Pac>();

        foreach (var kvp in myPacs)
        {
            var pacId = kvp.Key;
            var pac = kvp.Value;

            if (pac.abilityCooldown == 0)
            {
                pac.ActivateSpeed();
                continue;
            }
            
            if(pac.HasMove == false)
            {
                //pac.Move();

                var bestZone = myZones
                    .Where(kvp => kvp.Value.pacId == pacId)
                    .Select(kvp => kvp.Value)
                    .OrderByDescending(zone => zone.Score)
                    .First();

                pac.Move(bestZone.direction);

                if(pac.speedTurnsLeft > 0)
                {
                    //compute second turn
                    myPacsWithSpeed.Add(pac);
                }
            } 
        }

        if(myPacsWithSpeed.Count > 0)
        {
            Player.Debug("-----------------------------");
            
            var secondTurnZones = RunSimulation(myPacsWithSpeed, GameState.enemyPacs.Values.ToList());
            foreach(var pac in myPacsWithSpeed)
            {
                var bestZone = secondTurnZones
                    .Where(kvp => kvp.Value.pacId == pac.pacId)
                    .Select(kvp => kvp.Value)
                    .OrderByDescending(zone => zone.Score)
                    .First();
                Player.Debug($"Player {pac.pacId} ({pac.x}, {pac.y}) moves to {bestZone.direction}");
                pac.Move(bestZone.direction);
            }

        }


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

        while (turn <= 10)
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

public class Zone
{
    public static Guid NeutralZoneId = new Guid("{B7BAD255-8A34-4870-BA36-BF813F896BA6}");

    public Guid Id { get; }
    
    public int pacId { get; }

    public Direction direction { get; }

    private List<(int,int)> Frontier = new List<(int,int)>();

    public double Score = 0;

    /// <summary>
    /// Coords in the zone
    /// </summary>
    private HashSet<(int, int)> Coords = new HashSet<(int, int)>();

    public Zone(Pac pac, Direction direction, (int,int) start)
    {
        this.Id = Guid.NewGuid();
        this.pacId = pac.pacId;
        this.direction = direction;

        Map.Cells[pac.Coord].OwnedByZone = NeutralZoneId;
        Map.Cells[start].OwnedByZone = this.Id;

        Frontier.Add(start);
    }

    public List<(int,int)> Expand()
    {
        var newFrontier = new List<(int, int)>();

        foreach(var currentPos in Frontier)
        {
            foreach(var neighbor in Map.Cells[currentPos].Neighbors)
            {
                var neighborCell = neighbor.Value;
                if(neighborCell.OwnedByZone.HasValue == false)
                {
                    neighborCell.Color(this.Id);
                    newFrontier.Add(neighborCell.Coord);
                }
            }
        }

        this.Frontier = newFrontier;

        return newFrontier;
    }
         
    public void AddCoord((int,int) coord, int turn)
    {
        Coords.Add(coord);

        Score += Map.Cells[coord].PelletValue;
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
