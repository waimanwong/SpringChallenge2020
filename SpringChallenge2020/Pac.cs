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

    private Action currentAction;

    private Dictionary<Direction, List<Pellet>> visiblePellets = new Dictionary<Direction, List<Pellet>>();

    private Dictionary<Direction, int> possibleScores = new Dictionary<Direction, int>();

    public Direction? bestDirectionForPellets;

    public Pac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown): base(x,y)
    {
        this.pacId = pacId;
        this.mine = mine;
        this.typeId = typeId;
        this.speedTurnsLeft = speedTurnsLeft;
        this.abilityCooldown = abilityCooldown;
    }

    public void UpdateState(Pac visiblePac, Dictionary<(int, int), Pellet> visiblePellets)
    {
        this.x = visiblePac.x;
        this.y = visiblePac.y;
        this.typeId = visiblePac.typeId;
        this.speedTurnsLeft = visiblePac.speedTurnsLeft;
        this.abilityCooldown = visiblePac.abilityCooldown;

        CheckCurrentActionCompletion();

        SetVisiblePellets(visiblePellets);
    }

    private void CheckCurrentActionCompletion()
    {
        if (currentAction.IsCompleted(this))
        {
            this.currentAction = null;
        }
    }

    private void SetVisiblePellets(Dictionary<(int, int), Pellet> visiblePellets)
    {
        bestDirectionForPellets = null;
        this.visiblePellets.Clear();

        int bestScore = 0;

        foreach (var direction in new[] { Direction.East, Direction.North, Direction.South, Direction.West })
        {
            var currentCell = Map.Cells[(this.x, this.y)];
            var pellets = new List<Pellet>();

            while (currentCell.Neighbors.TryGetValue(direction, out var nextCell))
            {
                currentCell = nextCell;
                if (visiblePellets.TryGetValue(currentCell.Coord, out var visiblePellet))
                {
                    pellets.Add(visiblePellet);
                }
            }

            this.visiblePellets[direction] = pellets;
            this.possibleScores[direction] = pellets.Sum(p => p.value);

            if(possibleScores[direction] > bestScore)
            {
                bestScore = possibleScores[direction];
                bestDirectionForPellets = direction;
            }
        }
    }

    public bool HasAction => currentAction != null;

    public void AssignMoveAction(int x, int y)
    {
        this.currentAction = new Move(this.pacId, x, y);
    }

    public void ClearAction()
    {
        this.currentAction = null;
    }

    public string GetCommand()
    {
        return this.currentAction.ToString();
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
