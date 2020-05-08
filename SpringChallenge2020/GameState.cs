using System.Collections.Generic;
using System.Linq;

public static class GameState
{
    public static Dictionary<int, Move> CurrentMoves = new Dictionary<int, Move>();

    public static Dictionary<int, Pac> myPacs;
    public static Dictionary<int, Pac> enemyPacs;

    public static int myScore, opponentScore;

    public static Dictionary<(int, int), Pellet> visiblePellets;

    public static HashSet<int> MyPacsDidNotMove;

    public static string GetMoves()
    {
        return string.Join('|',
            CurrentMoves.Values.Select(m => m.ToString()));
           
    }

    static GameState()
    {
        myPacs = new Dictionary<int, Pac>();
    }

    public static void SetState(int myScore, int opponentScore, List<Pac> visiblePacs, List<Pellet> visiblePellets)
    {
        GameState.myScore = myScore;
        GameState.opponentScore = opponentScore;

        var newEnemyPacs = new Dictionary<int, Pac>();

        GameState.MyPacsDidNotMove = new HashSet<int>();

        foreach (var newVisiblePac in visiblePacs)
        {
            var pacId = newVisiblePac.pacId;

            if(newVisiblePac.mine)
            {
                if(myPacs.TryGetValue(pacId, out var currentPac))
                {
                    if(currentPac.x == newVisiblePac.x && currentPac.y == newVisiblePac.y)
                    {
                        GameState.MyPacsDidNotMove.Add(pacId);
                    }
                    myPacs[pacId] = newVisiblePac;
                }
                else
                {
                    myPacs[pacId] = newVisiblePac;
                }
            }
            else
            {
                newEnemyPacs[pacId] = newVisiblePac;
            }
        }

        GameState.enemyPacs = newEnemyPacs;

        GameState.visiblePellets = visiblePellets.ToDictionary(
            keySelector: pellet => pellet.Coord,
            elementSelector:  pellet => pellet);
    }

}
