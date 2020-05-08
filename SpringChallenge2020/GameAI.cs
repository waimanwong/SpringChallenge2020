using System;
using System.Collections.Generic;
using System.Linq;



public class GameAI
{
    private readonly Map map;
    private readonly GameState gameState;

    public GameAI(Map map, GameState gameState)
    {
        this.map = map;
        this.gameState = gameState;
    }

    public void ComputeMoves()
    {
        var myPacs = gameState.myPacs.ToList();
        var pellets = gameState.visiblePellets.Values.ToList();

        var random = new Random();
        foreach (var pac in myPacs)
        {
            if (GameState.CurrentMoves.TryGetValue(pac.pacId, out var existingMove))
            {
                // Check pellet is still here
                if (gameState.visiblePellets.ContainsKey(existingMove.Coord) == false)
                {
                    //assign a new move
                    AssignMoveToPac(pellets, random, pac);
                }
            }
            else
            {
                //Assign a move to this pac
                AssignMoveToPac(pellets, random, pac);
            }

        }
    }

    private static void AssignMoveToPac(List<Pellet> pellets, Random random, Pac pac)
    {
        var randomPellet = pellets[random.Next(pellets.Count)];
        var move = new Move(pac.pacId, randomPellet.x, randomPellet.y);

        GameState.CurrentMoves[pac.pacId] = move;

        pellets.Remove(randomPellet);
    }
}
