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

    private string recommendedType;

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
        this.recommendedType = string.Empty;

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

    private void SetBestDirectionForPellets(
        Dictionary<int, Pac> myVisiblePacsById, 
        Dictionary<(int, int), Pellet> visiblePellets)
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

        if(speedTurnsLeft > 0)
        {
            //Can move one cell further
            this.x = cell.x;
            this.y = cell.y;

            if(Map.Cells[this.Coord].Neighbors.TryGetValue(choosenDirection, out var secondCell))
            {
                //keep going in the same direction
                this.currentMove = new Move(this.pacId, secondCell.x, secondCell.y);
                return;
            }

            this.Behavior = Behavior.RandomMove;

            var possibleNeighbors = Map.Cells[this.Coord].Neighbors.Select(kvp => kvp.Value.Coord).ToHashSet();
                
            // priority to not visited
            foreach (var possibleNeighbor in possibleNeighbors)
            {
                if(GameState.PositionsToVisit.Contains(possibleNeighbor))
                {
                    //Second move here
                    this.currentMove = new Move(this.pacId, possibleNeighbor.Item1, possibleNeighbor.Item2);
                    return;
                }
            }

            //Otherwise...
            var defaultChoice = possibleNeighbors.First();
            this.currentMove = new Move(this.pacId, defaultChoice.Item1, defaultChoice.Item2);
            return;
        }

    }

    public void RandomMoveTo(Random random)
    {
        var (targetX, targetY) = GameState.GetRandomCellToVisit(random);

        this.currentMove = new Move(this.pacId, targetX, targetY);
    }

    public void SwitchToType(string recommendedType)
    {
        this.recommendedType = recommendedType;
    }

    public void ActivateSpeed()
    {
        this.activateSpeed = true;
    }

    public string GetCommand()
    {
        if(string.IsNullOrEmpty(this.recommendedType) == false)
        {
            return $"SWITCH {this.pacId.ToString()} {this.recommendedType}";
        }

        if( this.activateSpeed )
        {
            return $"SPEED {this.pacId.ToString()} {this.Behavior.ToString()}";
        }

        var message = $"{this.Behavior.ToString().First()} {this.currentMove.x} {this.currentMove.y}";

        return $"{this.currentMove.ToString()} {message}";
    }


}
