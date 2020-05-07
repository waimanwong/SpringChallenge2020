public class Pellet: Position
{
    public readonly int value;

    public Pellet(int x, int y, int value): base(x,y)
    {
        this.value = value;
    }
}
