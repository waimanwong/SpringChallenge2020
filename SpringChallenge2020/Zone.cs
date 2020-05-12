using System;
using System.Collections.Generic;
using System.Linq;

public class Zone
{
    public static Guid NeutralZoneId = new Guid("{B7BAD255-8A34-4870-BA36-BF813F896BA6}");

    public Guid Id { get; }

    public int pacId { get; }

    public Direction direction { get; }

    private List<(int, int)> Frontier = new List<(int, int)>();

    /// <summary>
    /// Coords in the zone
    /// </summary>
    private HashSet<(int, int)> Coords = new HashSet<(int, int)>();

    public Zone(Pac pac, Direction direction, (int, int) start)
    {
        this.Id = Guid.NewGuid();
        this.pacId = pac.pacId;
        this.direction = direction;

        Map.Cells[pac.Coord].OwnedByZone = NeutralZoneId;
        Map.Cells[start].OwnedByZone = this.Id;

        AddCoord(start, 0);

        Frontier.Add(start);
    }

    public List<(int, int)> Expand()
    {
        var newFrontier = new List<(int, int)>();

        foreach (var currentPos in Frontier)
        {
            foreach (var neighbor in Map.Cells[currentPos].Neighbors)
            {
                var neighborCell = neighbor.Value;
                if (neighborCell.OwnedByZone.HasValue == false)
                {
                    neighborCell.Color(this.Id);
                    newFrontier.Add(neighborCell.Coord);
                }
            }
        }

        this.Frontier = newFrontier;

        return newFrontier;
    }

    private Dictionary<int, List<(int, int)>> frontiers = new Dictionary<int, List<(int, int)>>();

    public void AddCoord((int, int) coord, int turn)
    {
        Coords.Add(coord);

        if (frontiers.ContainsKey(turn) == false)
            frontiers[turn] = new List<(int, int)>();

        frontiers[turn].Add(coord);
    }

    public double Score 
    { 
        get 
        {
            double score = 0;
            foreach(var kvp in frontiers)
            {
                var maxScoreAtFrontier = kvp.Value
                    .Select(coord => Map.Cells[coord].PelletValue)
                    .OrderByDescending(v => v)
                    .First();
                var turn = kvp.Key;

                score += maxScoreAtFrontier * Math.Pow(10, -turn);
            }

            return score;
        } 
    }
        

    public void Debug()
    {
        Player.Debug($"Zone {pacId} {direction.ToString()} score = {this.Score}");

        //var row = new StringBuilder();

        //for (int y = 0; y < Map.Height; y++)
        //{
        //    row.Clear();
        //    for (int x = 0; x < Map.Width; x++)
        //    {
        //        var coord = (x, y);
        //        if (Map.Cells.TryGetValue(coord, out var cell))
        //        {
        //            if(cell.OwnedByZone.HasValue && cell.OwnedByZone.Value == this.Id)
        //            {
        //                row.Append($"{this.pacId}");
        //            }
        //            else
        //            {
        //                row.Append(' ');
        //            }
        //        }
        //        else
        //            row.Append('#');
        //    }
        //    Player.Debug(row.ToString());
        //}
    }
}
