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

    }

    private void CheckIfBlocked(Pac visiblePac)
    {
        var lastActionIsMove = (this.activateSpeed == false);
       isBlocked = lastActionIsMove && this.x == visiblePac.x && this.y == visiblePac.y; 
    }

    public List<Pac> VisiblePacs;

    public void UpdateVisibleEnemyPacs(Dictionary<int, Pac> enemyVisiblePacsById)
    {
        VisiblePacs = new List<Pac>();
        var currentCoord = this.Coord;
        var visibleEnemies = enemyVisiblePacsById.Values.ToList();

        foreach(var direction in new[] { Direction.East, Direction.North, Direction.South, Direction.West})
        {
            while(Map.Cells[currentCoord].Neighbors.TryGetValue(direction, out var newCell))
            {
                currentCoord = newCell.Coord;
                var visibleEnemy = visibleEnemies.SingleOrDefault(p => p.Coord == currentCoord);
                if (visibleEnemy != null)
                {
                    VisiblePacs.Add(visibleEnemy);
                }
            }
        }
    }

    public void Move(Direction direction)
    {
        var cell = Map.Cells[this.Coord].Neighbors[direction];

        this.currentMove = new Move(this.pacId, cell.x, cell.y);
        
        this.x = cell.x;
        this.y = cell.y;

        Map.Cells[this.Coord].PelletValue = 0;
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
