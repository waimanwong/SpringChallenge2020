using System;
using System.Linq;

public class GameAI
{
    public void ComputeActions()
    {
        var myPacs = GameState.myPacs.ToList();
        var random = new Random();

        foreach (var kvp in myPacs)
        {
            var pacId = kvp.Key;
            var pac = kvp.Value;

            if (pac.HasAction == false)
            {
                // Assign a move to this pac
                Player.Debug($"Assign a move to this pac {pacId.ToString()}");
                AssignMoveToPac(random, pac);
            }
        }
    }

    private void AssignMoveToPac(Random random, Pac pac)
    {
        if(pac.bestDirectionForPellets != null)
        {
            var choosenDirection = pac.bestDirectionForPellets.Value;
            var pelletsToCollect = pac.visiblePellets[choosenDirection];

            var lastPellet = pelletsToCollect.Peek();

            pac.AssignMoveAction(lastPellet.x, lastPellet.y);
        }
        else
        {
            var (x, y) = GameState.GetRandomCellToVisit(random);

            pac.AssignMoveAction(x, y);
        }
    }
}
