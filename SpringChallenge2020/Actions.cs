using System.Runtime.Serialization;

public abstract class Action 
{
    public int pacId;

    public abstract bool IsCompleted(Pac pac);

    public Action(int pacId)
    {
        this.pacId = pacId;
    }
} 

public class Speed : Action
{
    public Speed(int pacId): base(pacId)
    {  
    }
    public override bool IsCompleted(Pac pac)
    {
        return true;
    }
    public override string ToString()
    {
        return $"SPEED {pacId.ToString()}";
    }
}

public class Switch : Action
{
    public string pacType;

    public Switch(int pacId, string pacType): base(pacId)
    {
        this.pacType = pacType;
    }
    public override bool IsCompleted(Pac pac)
    {
        return true;
    }
    public override string ToString()
    {
        return $"SWITCH {pacId.ToString()} {pacType.ToString()}";
    }
}

public class Move : Action
{
    public int x;
    public int y;

    public Move(int pacId, int x, int y): base(pacId)
    {
        this.x = x;
        this.y = y;
    }

    public (int, int) Coord => (x, y);
    public override bool IsCompleted(Pac pac)
    {
        return pac.x == this.x && pac.y == this.y;
    }
    public override string ToString()
    {
        return $"MOVE {pacId.ToString()} {x.ToString()} {y.ToString()}";
    }
}
