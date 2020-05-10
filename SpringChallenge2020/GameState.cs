using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class GameState
{
    public readonly static Dictionary<int, Pac> myPacs;
    public static Dictionary<int, Pac> enemyPacs;

    public static int myScore, opponentScore;

    public static Dictionary<(int, int), Pellet> visiblePellets;

    public static HashSet<(int, int)> PositionsToVisit;

    static GameState()
    {
        myPacs = new Dictionary<int, Pac>();
    }

    public static void InitializeRemainingCellsToVisit()
    {
        PositionsToVisit = Map.Cells.Keys.ToHashSet();
    }

    public static (int,int) GetRandomCellToVisit(Random random)
    {
        var randomIndex = random.Next(PositionsToVisit.Count);

        return PositionsToVisit.ElementAt(randomIndex);
    }

    public static void SetState(
        int myScore, int opponentScore, 
        Dictionary<int, Pac> myVisiblePacsById, 
        Dictionary<int, Pac> enemyVisiblePacsById,
        Dictionary<(int, int), Pellet> visiblePellets)
    {
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
        var enemyKnownPositions = enemyVisiblePacsById.Values.Select(p => p.Coord).ToHashSet();
        var visiblePellets = visiblePelletsByCoord.Keys.ToHashSet();

        foreach (var kvp in myVisiblePacsById)
        {
            var visiblePac = kvp.Value;
            var visitedCoord = (visiblePac.x, visiblePac.y);

            if (PositionsToVisit.Contains(visitedCoord))
            {
                PositionsToVisit.Remove(visitedCoord);
            }

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

                    if (PositionsToVisit.Contains(currentCell.Coord))
                    {
                        var thereIsAnEnemyAtCurrentCell = enemyKnownPositions.Contains(currentCell.Coord);
                        var thereIsNotPelletAtCurrentCell = visiblePellets.Contains(currentCell.Coord) == false;

                        if(thereIsAnEnemyAtCurrentCell || thereIsNotPelletAtCurrentCell)
                        {
                            //place is visited
                            PositionsToVisit.Remove(currentCell.Coord);
                        }
                    }
                }
            }
        }

        foreach(var enemyKnownPosition in enemyKnownPositions)
        {
            if (PositionsToVisit.Contains(enemyKnownPosition))
                PositionsToVisit.Remove(enemyKnownPosition);
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
                    if(PositionsToVisit.Contains(coord))
                    {
                        row.Append('.');
                    }
                    else
                    {
                        row.Append(' ');
                    }

                    //if (visiblePellets.TryGetValue(coord, out var pellet))
                    //{
                    //    row.Append(pellet.value == 1 ? 'o' : 'O');
                    //}
                    //else if(myPacs.TryGetValue(coord, out var myPac))
                    //{
                    //    row.Append(myPac.pacId.ToString());
                    //}
                    //else if (enemyPacs.TryGetValue(coord, out var enemyPac))
                    //{
                    //    row.Append('!');
                    //}
                    //else
                    //{
                    //    row.Append(' ');
                    //}
                }
                else
                    row.Append('#');
            }
            Player.Debug(row.ToString());
        }
    }

}
