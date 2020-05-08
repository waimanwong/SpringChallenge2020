using System;

public class Pac: Position
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
