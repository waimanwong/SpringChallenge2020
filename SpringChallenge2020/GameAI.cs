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

            if (pac.abilityCooldown == 0)
            {
                pac.ActivateSpeed();
            }
            else
            {
                if (pac.IsBlocked)
                {
                    Unblock(pac, random);
                }
                else
                {
                    if(pac.Behavior == Behavior.RandomMove)
                    {
                        if (pac.bestDirectionForPellets != null)
                        {
                            var choosenDirection = pac.bestDirectionForPellets.Value;
                            
                            var cell = Map.Cells[pac.Coord].Neighbors[choosenDirection];
                            pac.CollectPelletTo(cell.x, cell.y);
                        }
                    }

                    if (pac.HasAction == false)
                    {
                        // Assign a move to this pac
                        
                        AssignMoveToPac(pac, random);
                    }
                }
            }
        }
    }

    private void Unblock(Pac pac, Random random)
    {
        Player.Debug($"Unblock {pac.pacId.ToString()}");
        pac.RandomMoveTo(random);
    }

    private void AssignMoveToPac(Pac pac, Random random)
    {
        Player.Debug($"Assign a move to this pac {pac.pacId.ToString()}");
        if (pac.bestDirectionForPellets != null)
        {
            var choosenDirection = pac.bestDirectionForPellets.Value;
            //var pelletsToCollect = pac.visiblePellets[choosenDirection];

            //var lastPellet = pelletsToCollect.Peek();

            //pac.CollectPelletTo(lastPellet.x, lastPellet.y);


            var cell = Map.Cells[pac.Coord].Neighbors[choosenDirection];
            pac.CollectPelletTo(cell.x, cell.y);

        }
        else
        {
            pac.RandomMoveTo(random);
        }
    }
}
