using System;
using System.Collections.Generic;
using System.ComponentModel;

/**
 * Grab the pellets as fast as you can!
 **/
public class Player
{
    public static void Debug(string message)
    {
        Console.Error.WriteLine(message);
    }

    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]); // size of the grid
        int height = int.Parse(inputs[1]); // top left corner is (x=0, y=0)

        var rows = new List<string>(height);

        for (int i = 0; i < height; i++)
        {
            string row = Console.ReadLine(); // one line of the grid: space " " is floor, pound "#" is wall
            rows.Add(row);
        }

        var map = new Map(width, height, rows);
       
        // game loop
        while (true)
        {   
            inputs = Console.ReadLine().Split(' ');
            int myScore = int.Parse(inputs[0]);
            int opponentScore = int.Parse(inputs[1]);

            int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
            var pacs = new List<Pac>(visiblePacCount);
            for (int i = 0; i < visiblePacCount; i++)
            {
                var pacState = Console.ReadLine();

                inputs = pacState.Split(' ');
                int pacId = int.Parse(inputs[0]); // pac number (unique within a team)
                bool mine = inputs[1] != "0"; // true if this pac is yours
                int x = int.Parse(inputs[2]); // position in the grid
                int y = int.Parse(inputs[3]); // position in the grid
                string typeId = inputs[4]; // unused in wood leagues
                int speedTurnsLeft = int.Parse(inputs[5]); // unused in wood leagues
                int abilityCooldown = int.Parse(inputs[6]); // unused in wood leagues

                pacs.Add(new Pac(pacId, mine, x, y, typeId, speedTurnsLeft, abilityCooldown));
            }


            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            var pellets = new List<Pellet>(visiblePelletCount);
            for (int i = 0; i < visiblePelletCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth

                pellets.Add(new Pellet(x, y, value));
            }

            var gameState = new GameState(myScore, opponentScore, pacs, pellets);

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            var gameAI = new GameAI(map, gameState);
            gameAI.ComputeMoves();

            Console.WriteLine(GameState.GetMoves()); // MOVE <pacId> <x> <y>

        }
    }
}