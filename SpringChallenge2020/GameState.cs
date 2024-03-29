﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

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

            myPacs[pacId].UpdateVisibleEnemyPacs(enemyVisiblePacsById);
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
