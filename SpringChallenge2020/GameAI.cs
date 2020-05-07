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

    public string ComputeAction()
    {
        var myPac = gameState.myPacs.Single();

        var targetPellet = gameState.visiblePellets
            .OrderByDescending(p => p.value)
            .ThenBy(pellet => pellet.DistanceTo(myPac, this.map))
            .First();

        return $"MOVE {myPac.pacId.ToString()} {targetPellet.x.ToString()} {targetPellet.y.ToString()}";
    }
}
