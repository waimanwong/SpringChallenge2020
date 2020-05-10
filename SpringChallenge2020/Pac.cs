using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;


public enum Behavior
{
    None,
    RandomMove,
    CollectPellet
}

public class Pac: Position
{
    public readonly int pacId;
    public readonly bool mine;
  
    public string typeId; // unused in wood leagues
    public int speedTurnsLeft; // unused in wood leagues

    public int abilityCooldown; // unused in wood leagues

    private Action currentAction;

    public Dictionary<Direction, Stack<Pellet>> visiblePellets = new Dictionary<Direction, Stack<Pellet>>();
    public Direction? bestDirectionForPellets;
    public Behavior Behavior;
    public bool IsBlocked = false;

    public Pac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown): base(x,y)
    {
        this.pacId = pacId;
        this.mine = mine;
        this.typeId = typeId;
        this.speedTurnsLeft = speedTurnsLeft;
        this.abilityCooldown = abilityCooldown;

        this.Behavior = Behavior.None;

    }

    public void UpdateState(Pac visiblePac, 
        Dictionary<int, Pac> myVisiblePacs,
        Dictionary<int, Pac> enemyVisiblePacs, 
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        CheckIfBlocked(visiblePac);

        this.x = visiblePac.x;
        this.y = visiblePac.y;

        this.typeId = visiblePac.typeId;
        this.speedTurnsLeft = visiblePac.speedTurnsLeft;
        this.abilityCooldown = visiblePac.abilityCooldown;

        CheckCurrentActionCompletion();

    }

    private void CheckIfBlocked(Pac visiblePac)
    {
       IsBlocked = this.x == visiblePac.x && this.y == visiblePac.y; 
    }

    private void CheckCurrentActionCompletion()
    {
        if (currentAction.IsCompleted(this))
        {
            this.currentAction = null;
        }
    }

    public void SetVisiblePellets(
        Dictionary<int, Pac> myVisiblePacsById,
        Dictionary<int, Pac> enemyVisiblePacsbyId, 
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        bestDirectionForPellets = null;
        this.visiblePellets.Clear();

        int bestScore = 0;

        var myVisiblePacs = myVisiblePacsById.Values
            .ToDictionary(keySelector: pac => pac.Coord, elementSelector: pac => pac);

        foreach (var direction in new[] { Direction.East, Direction.North, Direction.South, Direction.West })
        {
            var currentCell = Map.Cells[(this.x, this.y)];
            var pellets = new Stack<Pellet>();

            while (currentCell.Neighbors.TryGetValue(direction, out var nextCell))
            {
                currentCell = nextCell;

                if(myVisiblePacs.TryGetValue(currentCell.Coord, out var myBlockingPac))
                {
                    //Block in one way
                    if( myBlockingPac.x < this.x || myBlockingPac.y < this.y)
                    {
                        break;
                    }
                }

                if (visiblePellets.TryGetValue(currentCell.Coord, out var visiblePellet))
                {
                    pellets.Push(visiblePellet);
                }
            }

            this.visiblePellets[direction] = pellets;
            int score = pellets.Sum(p => p.value);

            if(score > bestScore)
            {
                bestScore = score;
                bestDirectionForPellets = direction;
            }
        }
    }

    public bool HasAction => currentAction != null;

    public void CollectPelletTo(int x, int y)
    {
        this.Behavior = Behavior.CollectPellet;
        this.currentAction = new Move(this.pacId, x, y);

        Player.Debug($"\tCollectPelletTo ({x},{y})");
    }

    public void RandomMoveTo(Random random)
    {
        var (x, y) = GameState.GetRandomCellToVisit(random);

        this.Behavior = Behavior.RandomMove;
        this.currentAction = new Move(this.pacId, x, y);

        Player.Debug($"\tRandomMoveTo ({x},{y})");
    }

    public void ActivateSpeed()
    {
        this.currentAction = new Speed(this.pacId);
    }

    public string GetCommand()
    {
        return $"{this.currentAction.ToString()} {this.Behavior.ToString()}";
    }

    public const string ROCK = "ROCK";
    public const string PAPER = "PAPER";
    public const string SCISSORS = "SCISSORS";

    public int Compare(Pac enemyPac)
    {
        var myType = this.typeId;
        var enemyType = enemyPac.typeId;

        switch(myType)
        {
            case ROCK:
                switch(enemyType)
                {
                    case ROCK:
                        return 0;
                    case PAPER:
                        return -1;
                    case SCISSORS:
                        return 1;
                }
                break;

            case "PAPER":
                switch (enemyType)
                {
                    case ROCK:
                        return 1;
                    case PAPER:
                        return 0;
                    case SCISSORS:
                        return -1;
                }
                break;

            case "SCISSORS":
                switch (enemyType)
                {
                    case ROCK:
                        return -1;
                    case PAPER:
                        return 1;
                    case SCISSORS:
                        return 0;
                }
                break;
        }

        throw new Exception();
    }

}
