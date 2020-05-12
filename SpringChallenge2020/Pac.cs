using System;
using System.Collections.Generic;
using System.Linq;

public class Pac: Position
{
    public readonly int pacId;
    public readonly bool mine;
  
    public string typeId; // unused in wood leagues
    public int speedTurnsLeft; // unused in wood leagues

    public int abilityCooldown; // unused in wood leagues

    private Move currentMove;
    private bool activateSpeed = false;

    private string newType;

    public Direction? bestDirection;

    private bool isBlocked = false;

    public Pac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown): base(x,y)
    {
        this.pacId = pacId;
        this.mine = mine;
        this.typeId = typeId;
        this.speedTurnsLeft = speedTurnsLeft;
        this.abilityCooldown = abilityCooldown;
    }

    public bool IsDead => typeId == "DEAD";

    public void UpdateState(Pac visiblePac)
    {
        CheckIfBlocked(visiblePac);

        this.x = visiblePac.x;
        this.y = visiblePac.y;

        this.typeId = visiblePac.typeId;
        this.speedTurnsLeft = visiblePac.speedTurnsLeft;
        this.abilityCooldown = visiblePac.abilityCooldown;

        this.activateSpeed = false;
        this.newType = string.Empty;

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

    public void ComputeBestDirection(
        Dictionary<int, Pac> myVisiblePacsById,
        Dictionary<int, Pac> enemyVisiblePacsbyId, 
        Dictionary<(int, int), Pellet> visiblePellets)
    {
        this.bestDirection = null;
        double bestScore = int.MinValue;

        var myVisiblePacs = myVisiblePacsById.Values
            .ToDictionary(keySelector: pac => pac.Coord, elementSelector: pac => pac);

        var enemyVisiblePacs = enemyVisiblePacsbyId.Values
            .ToDictionary(keySelector: pac => pac.Coord, elementSelector: pac => pac);

        //Player.Debug($"Compute best direction for pac {this.pacId}");

        //Compute best direction
        foreach (var direction in new[] { Direction.East, Direction.North, Direction.South, Direction.West })
        {   
            if(Map.Cells[(this.x, this.y)].Neighbors.ContainsKey(direction) == false)
            {
                //can not go in this direction, it is a wall
                continue;
            }

            var distance = 0;
            var currentCell = Map.Cells[(this.x, this.y)];
            double directionScore = 0;
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
                    //There is a pac of mine in this which blocks 
                    break;
                }

                if(enemyVisiblePacs.TryGetValue(currentCell.Coord, out var enemyPac))
                {
                    //by default it is a threat if too close
                    if (distance < 4)
                    {
                        directionScore -= 100;
                    }
                    break;
                }

                if (visiblePellets.TryGetValue(currentCell.Coord, out var visiblePellet))
                {
                    directionScore += (visiblePellet.value * Math.Pow(10, -distance));
                }
            }

            //Player.Debug($"\tDirection: {direction}: {directionScore.ToString()}");

            if (directionScore > bestScore)
            {
                bestScore = directionScore;
                bestDirection = direction;
            }
        }

    }

    public bool HasMove => currentMove != null;

    public void Move(Direction direction)
    {
        var cell = Map.Cells[this.Coord].Neighbors[direction];

        this.currentMove = new Move(this.pacId, cell.x, cell.y);
        
        this.x = cell.x;
        this.y = cell.y;

    }

    public void SwitchToType(string newType)
    {
        this.newType = newType;
    }

    public void ActivateSpeed()
    {
        this.activateSpeed = true;
    }

    public string GetCommand()
    {
        if(string.IsNullOrEmpty(this.newType) == false)
        {
            return $"SWITCH {this.pacId.ToString()} {this.newType}";
        }

        if( this.activateSpeed )
        {
            return $"SPEED {this.pacId.ToString()}";
        }

        return $"{this.currentMove.ToString()}";
    }


}
