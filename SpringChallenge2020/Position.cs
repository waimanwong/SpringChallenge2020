using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

public abstract class Position
{
    public int x; 
    public int y;

    protected Position (int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public (int, int) Coord => (x, y);

    public int DistanceTo(Position other, Map map)
    {
        //BFS
        var visited = new HashSet<(int, int)>();
        var frontier = new Queue<(int,Position)>();
        var distance = 0;

        frontier.Enqueue((0, this));

        while(frontier.Count > 0)
        {
            var (currentDistance, currentPosition) = frontier.Dequeue();

            if(currentPosition.Coord == other.Coord)
            {
                distance = currentDistance;
                break;
            }

            visited.Add(currentPosition.Coord);

            var neighbors = map.GetNeighbors(currentPosition);

            foreach(var neighborCell in neighbors)
            {
                if(visited.Contains(neighborCell.Coord) == false)
                {
                    frontier.Enqueue((currentDistance + 1, neighborCell));
                }
            }
        }

        return distance;
    }

}
