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

        //Those can have one move
        var pacsWithOneMove = myPacs.Where(kvp => Map.Cells[kvp.Value.Coord].Neighbors.Count == 1).ToList();
        var pacIdsToExclude = pacsWithOneMove.Select(kvp => kvp.Key).ToHashSet();
        foreach(var kvp in pacsWithOneMove)
        {
            var pacWithOneMove = kvp.Value;
            pacWithOneMove.Move(Map.Cells[pacWithOneMove.Coord].Neighbors.Single().Key);
        }


        foreach (var kvp in myPacs.Where(kvp => pacIdsToExclude.Contains( kvp.Key)==false))
        {
            var pacId = kvp.Key;
            var pac = kvp.Value;

            if (pac.VisiblePacs.Count > 0)
            {
                var (closestEnemy, distance) = pac.VisiblePacs.OrderBy(kvp => kvp.Item2).First();
                if(distance == 1)
                {
                    var typeComparison = TypeAnalyzer.Compare(pac.typeId, closestEnemy.typeId);
                    var enemyIsStronger = typeComparison < 0;
                    var enemyIsWeaker = typeComparison > 0;
                    var sameType = typeComparison == 0;

                    if (enemyIsStronger)
                    {
                        if (pac.abilityCooldown == 0)
                        {
                            //enemy can not switch
                            var strongerType = TypeAnalyzer.GetStrongerType(closestEnemy.typeId);
                            pac.SwitchToType(strongerType);
                            continue;
                        }
                        else
                        {
                            //Flee
                        }
                    }
                }
            }
            else
            {
                if (pac.abilityCooldown == 0)
                {
                    pac.ActivateSpeed();
                    continue;
                }
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
            Player.Debug("*************************************");
            Player.Debug("Second turn");

            var secondTurnZones = RunSimulation(
                GameState.myPacs.Values.ToList(),
                GameState.enemyPacs.Values.ToList());

            foreach(var pac in pacsWithSpeed)
            {
                var previousChoosenDirection = choosenDirection[pac.pacId];
                var oppositeDirection = Map.GetOppositeDirection(previousChoosenDirection);

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
