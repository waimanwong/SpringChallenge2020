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
            
            if(pac.HasMove == false)
            {
                //pac.Move();

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
        }

        if(pacsWithSpeed.Count > 0)
        {
            var secondTurnZones = RunSimulation(
                GameState.myPacs.Values.ToList(),
                GameState.enemyPacs.Values.ToList());

            foreach(var pac in pacsWithSpeed)
            {
                var zonesOfPac = secondTurnZones
                    .Where(kvp => kvp.Value.pacId == pac.pacId)
                    .ToList();
                if (zonesOfPac.Count == 1)
                {
                    pac.Move(zonesOfPac.Single().Value.direction);
                }
                else
                {
                    var previousChoosenDirection = choosenDirection[pac.pacId];
                    var oppositeDirection = GetOppositeDirection(previousChoosenDirection);

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
