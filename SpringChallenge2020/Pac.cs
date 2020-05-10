using System;
using System.Collections.Generic;
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

    private Move currentMove;
    private bool activateSpeed = false;
    
    public Direction? bestDirectionForPellets;

    private Behavior _behavior;
    private bool isBlocked = false;


    public Behavior Behavior { 
        get { return _behavior; }
        private set
        {
            if( this._behavior != value)
            {
                //Changing behavior => cancel current action
                currentMove = null;
            }
            this._behavior = value;
        } 
    }

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

        this.activateSpeed = false;

        CheckCurrentMoveCompletion();
    }

    private void CheckIfBlocked(Pac visiblePac)
    {
        var lastActionIsMove = (this.activateSpeed == false);
       isBlocked = lastActionIsMove && this.x == visiblePac.x && this.y == visiblePac.y; 
    }

    private void CheckCurrentMoveCompletion()
    {
        if (this.currentMove != null  && this.currentMove.IsCompleted(this))
        {
            this.currentMove = null;
        }

        if(this.isBlocked)
        {
            //Cancel current move
            this.currentMove = null;
        }
    }

    public Behavior ComputeBehavior(
        Dictionary<int, Pac> myVisiblePacsById,
        Dictionary<int, Pac> enemyVisiblePacsbyId, 
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        if (isBlocked)
        {
            Player.Debug($"{pacId} is blocked.");
            this.Behavior = Behavior.RandomMove;
            return this.Behavior;
        }
        
        SetBestDirectionForPellets(myVisiblePacsById, visiblePellets);

        if (this.bestDirectionForPellets == null)
        {
            this.Behavior = Behavior.RandomMove;
        }
        else
        {
            this.Behavior = Behavior.CollectPellet;
        }

        return this.Behavior;
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
            var visitedPosition = new HashSet<(int, int)>();

            visitedPosition.Add(currentCell.Coord);

            while (currentCell.Neighbors.TryGetValue(direction, out var nextCell))
            {
                if (visitedPosition.Contains(nextCell.Coord))
                    break;

                distance += 1;
                currentCell = nextCell;

                visitedPosition.Add(currentCell.Coord);

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

    public bool HasMove => currentMove != null;

    public void CollectPellet()
    {
        var choosenDirection = this.bestDirectionForPellets.Value;
        var cell = Map.Cells[this.Coord].Neighbors[choosenDirection];
        
        this.currentMove = new Move(this.pacId, cell.x, cell.y);

        Player.Debug($"\tCollectPelletTo ({cell.x},{cell.y})");
    }

    public void RandomMoveTo(Random random)
    {
        var (targetX, targetY) = GameState.GetRandomCellToVisit(random);

        this.currentMove = new Move(this.pacId, targetX, targetY);

        Player.Debug($"\tRandomMoveTo ({targetX},{targetY})");
    }

    public void ActivateSpeed()
    {
        this.activateSpeed = true;
    }

    public string GetCommand()
    {
        if( this.activateSpeed )
        {
            return $"SPEED {this.pacId.ToString()} {this.Behavior.ToString()}";
        }

        var message = $"{this.Behavior.ToString().First()} {this.currentMove.x} {this.currentMove.y}";

        return $"{this.currentMove.ToString()} {message}";
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
