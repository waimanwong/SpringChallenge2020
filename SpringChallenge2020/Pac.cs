﻿public class Pac: Position
{
    public readonly int pacId;
    public readonly bool mine;
  
    public readonly string typeId; // unused in wood leagues
    public readonly int speedTurnsLeft; // unused in wood leagues
    public readonly int abilityCooldown; // unused in wood leagues

    public Pac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown): base(x,y)
    {
        this.pacId = pacId;
        this.mine = mine;
        this.typeId = typeId;
        this.speedTurnsLeft = speedTurnsLeft;
        this.abilityCooldown = abilityCooldown;
    }

    public void Move(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public Pac Clone()
    {
        return new Pac(this.pacId,
                        this.mine,
                        this.x,
                        this.y,
                        this.typeId,
                        this.speedTurnsLeft,
                        this.abilityCooldown);
    }
}
