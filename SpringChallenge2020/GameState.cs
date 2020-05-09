using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

public static class GameState
{
    public readonly static Dictionary<int, Pac> myPacs;
    public static Dictionary<int, Pac> enemyPacs;

    public static int myScore, opponentScore;

    public static Dictionary<(int, int), Pellet> visiblePellets;

    public static HashSet<(int, int)> RemainingCellsToVisit;

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

    public static void SetState(
        int myScore, int opponentScore, 
        Dictionary<int, Pac> myVisiblePacs, 
        Dictionary<int, Pac> enemyVisiblePacs,
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        GameState.myScore = myScore;
        GameState.opponentScore = opponentScore;

        RemoveMyDeadPacman(myVisiblePacs);

        foreach (var kvp in myVisiblePacs)
        {
            var pacId = kvp.Key;
            var visiblePac = kvp.Value;

            if(myPacs.ContainsKey(pacId) == false)
            {
                myPacs[pacId] = visiblePac;
            }
            else
            {
                myPacs[pacId].UpdateState(visiblePac);
                
            }

            HasVisitedPosition(visiblePac);
        }

        GameState.enemyPacs = enemyVisiblePacs;
        GameState.visiblePellets = visiblePellets;
    }

    private static void RemoveMyDeadPacman(Dictionary<int, Pac> myVisiblePacs)
    {
        foreach (var kvp in myPacs)
        {
            var myPacId = kvp.Key;
            if (myVisiblePacs.ContainsKey(myPacId) == false)
            {
                myPacs.Remove(myPacId);
            }
        }
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
