using System.Collections.Generic;

public class GameState
{
    private readonly int myScore, opponentScore;

    public readonly List<Pac> myPacs;
    public readonly List<Pac> enemyPacs;

    public readonly List<Pellet> visiblePellets;

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

        this.visiblePellets = visiblePellets;
    }
}
