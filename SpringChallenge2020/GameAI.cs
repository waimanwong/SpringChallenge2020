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
                Player.Debug("Assign a move to this pac");
                AssignMoveToPac(random, pac);
            }
        }
    }

    private void AssignMoveToPac(Random random, Pac pac)
    {
        var (x,y) = GameState.GetRandomCellToVisit(random);

        pac.AssignMoveAction(x, y);
    }
}
