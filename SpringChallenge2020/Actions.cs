using System.Runtime.Serialization;

public class Move 
{
    public int pacId;
    public int x;
    public int y;

    public Move(int pacId, int x, int y)
    {
        this.pacId = pacId;
        this.x = x;
        this.y = y;
    }

    public (int, int) Coord => (x, y);
    public bool IsCompleted(Pac pac)
    {
        return pac.x == this.x && pac.y == this.y;
    }
    public override string ToString()
    {
        return $"MOVE {pacId.ToString()} {x.ToString()} {y.ToString()}";
    }
}
