public class Pellet: Position
{
    public readonly int value;

    public bool IsSuperPellet => value == 10;

    public Pellet(int x, int y, int value): base(x,y)
    {
        this.value = value;
    }
}
