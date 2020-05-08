using System.Collections.Generic;
using System.Linq;

public class GameState
{
    public static Dictionary<int, Move> CurrentMoves = 
        new Dictionary<int, Move>();

    public static string GetMoves()
    {
        return string.Join('|',
            CurrentMoves.Values.Select(m => m.ToString()));
           
    }

    public int myScore, opponentScore;

    public readonly List<Pac> myPacs;
    public readonly List<Pac> enemyPacs;

    public readonly Dictionary<(int,int),Pellet> visiblePellets;

    public GameState(int myScore, int opponentScore, List<Pac> visiblePacs, List<Pellet> visiblePellets)
    {
        this.myScore = myScore;
        this.opponentScore = opponentScore;

        this.myPacs = new List<Pac>(visiblePacs.Count);
        this.enemyPacs = new List<Pac>(visiblePacs.Count);

        foreach(var pac in visiblePacs)
        {
            if(pac.mine)
            {
                myPacs.Add(pac);
            }
            else
            {
                enemyPacs.Add(pac);
            }
        }

        this.visiblePellets = visiblePellets.ToDictionary(
            keySelector: pellet => pellet.Coord,
            elementSelector:  pellet => pellet);
    }

}
