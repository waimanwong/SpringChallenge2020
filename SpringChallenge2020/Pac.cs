using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;


public enum Behavior
{
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
    public Direction? bestDirectionForPellets;

    private Behavior _behavior;

    public Behavior Behavior { 
        get { return _behavior; }
        private set
        {
            if( this._behavior != value)
            {
                //Changing behavior => cancel current action
                currentAction = null;
            }
            this._behavior = value;
        } 
    }

    private bool isBlocked = false;

    public Pac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown): base(x,y)
    {
        this.pacId = pacId;
        this.mine = mine;
        this.typeId = typeId;
        this.speedTurnsLeft = speedTurnsLeft;
        this.abilityCooldown = abilityCooldown;

        this.Behavior = Behavior.RandomMove;

    }

    public void UpdateState(Pac visiblePac)
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
       isBlocked = this.x == visiblePac.x && this.y == visiblePac.y; 
    }

    private void CheckCurrentActionCompletion()
    {
        if (currentAction.IsCompleted(this))
        {
            this.currentAction = null;
        }
    }

    public void ComputeBehavior(
        Dictionary<int, Pac> myVisiblePacsById,
        Dictionary<int, Pac> enemyVisiblePacsbyId, 
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        if (isBlocked)
        {
            Player.Debug($"{pacId} is blocked.");
            this.Behavior = Behavior.RandomMove;
        }

        SetBestDirectionForPellets(myVisiblePacsById, visiblePellets);

        if(this.bestDirectionForPellets == null)
        {
            this.Behavior = Behavior.RandomMove;
        }
        else
        {
            this.Behavior = Behavior.CollectPellet;
        }
    }

    private void SetBestDirectionForPellets(Dictionary<int, Pac> myVisiblePacsById, Dictionary<(int, int), Pellet> visiblePellets)
    {
        bestDirectionForPellets = null;

        double bestScore = 0;

        var myVisiblePacs = myVisiblePacsById.Values
            .ToDictionary(keySelector: pac => pac.Coord, elementSelector: pac => pac);

        foreach (var direction in new[] { Direction.East, Direction.North, Direction.South, Direction.West })
        {
            
            var distance = 0;
            var currentCell = Map.Cells[(this.x, this.y)];
            double score = 0;

            while (currentCell.Neighbors.TryGetValue(direction, out var nextCell))
            {
                distance += 1;
                currentCell = nextCell;

                if (myVisiblePacs.TryGetValue(currentCell.Coord, out var myBlockingPac))
                {
                    //TO DO: more complicated logic (if enemy ...)
                    //Block in one way
                    if (myBlockingPac.x < this.x || myBlockingPac.y < this.y)
                    {
                        break;
                    }
                }

                if (visiblePellets.TryGetValue(currentCell.Coord, out var visiblePellet))
                {
                    score += (visiblePellet.value * Math.Pow(10, -distance));
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestDirectionForPellets = direction;
            }
        }
    }

    public bool HasAction => currentAction != null;

    public void CollectPellet()
    {
        var choosenDirection = this.bestDirectionForPellets.Value;
        var cell = Map.Cells[this.Coord].Neighbors[choosenDirection];
        
        this.currentAction = new Move(this.pacId, cell.x, cell.y);

        Player.Debug($"\tCollectPelletTo ({cell.x},{cell.y})");
    }

    public void RandomMoveTo(Random random)
    {
        var (targetX, targetY) = GameState.GetRandomCellToVisit(random);

        this.currentAction = new Move(this.pacId, targetX, targetY);

        Player.Debug($"\tRandomMoveTo ({targetX},{targetY})");
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
