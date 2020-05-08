using System;
using System.Linq;

public class GameAI
{
    private readonly Map map;

    public GameAI(Map map)
    {
        this.map = map;
    }

    public void ComputeMoves()
    {
        var myPacs = GameState.myPacs.ToList();
        var random = new Random();

        foreach (var kvp in myPacs)
        {
            var pacId = kvp.Key;
            var pac = kvp.Value;

            if(GameState.MyPacsDidNotMove.Contains(pacId))
            {
                //my pac is stuck, remove current move
                GameState.CurrentMoves.Remove(pacId);
            }

            if (GameState.CurrentMoves.TryGetValue(pacId, out var existingMove))
            {
                // Check pac is at destination
                if (pac.x == existingMove.x && pac.y == existingMove.y)
                {
                    //assign a new move
                    AssignMoveToPac(random, pac);
                }
            }
            else
            {
                //Assign a move to this pac
                AssignMoveToPac(random, pac);
            }
        }

        foreach (var kvp in GameState.CurrentMoves.ToArray())
        {
            if (myPacs.Any(p => p.Key == kvp.Key) == false)
            {
                //pac died: remove move
                GameState.CurrentMoves.Remove(kvp.Key);
            }
        }
    }

    private void AssignMoveToPac(Random random, Pac pac)
    {
        var randomCell = this.map.GetRandomCell(random);

        var move = new Move(pac.pacId, randomCell.x, randomCell.y);

        GameState.CurrentMoves[pac.pacId] = move;
    }
}
