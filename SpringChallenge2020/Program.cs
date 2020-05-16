using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        Map.Set(width, height, rows);

        int turn = 0;

        // game loop
        while (true)
        {
            turn++;

            inputs = Console.ReadLine().Split(' ');

            var watch = Stopwatch.StartNew();
            
            int myScore = int.Parse(inputs[0]);
            int opponentScore = int.Parse(inputs[1]);
            
            int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
            
            var enemyPacs = new Dictionary<int, Pac>(visiblePacCount);
            var myPacs = new Dictionary<int, Pac>(visiblePacCount);

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

                var pac = new Pac(pacId, mine, x, y, typeId, speedTurnsLeft, abilityCooldown);
                
                if (mine)
                {
                    myPacs.Add(pac.pacId, pac);

                    Player.Debug(pacState);
                }
                else
                {
                    enemyPacs.Add(pac.pacId, pac);
                }
            }

            if(turn == 1)
            {
                //Map is symetric so we know where the enemies are
                foreach(var kvp in myPacs)
                {
                    var myPac = kvp.Value;
                    var myPacId = kvp.Key;

                    if (enemyPacs.ContainsKey(myPacId) == false)
                    {
                        var symetricPac = new Pac(myPac.pacId, false, width - myPac.x - 1, myPac.y, myPac.typeId, myPac.speedTurnsLeft, myPac.abilityCooldown);
                        enemyPacs.Add(myPacId, symetricPac);
                    }
                }
                
            }

            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            var pellets = new Dictionary<(int,int), Pellet>(visiblePelletCount);
            for (int i = 0; i < visiblePelletCount; i++)
            {
                var pellet = Console.ReadLine();
                
                inputs = pellet.Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth

                pellets.Add((x,y), new Pellet(x, y, value));
            }

            GameState.SetState(turn, myScore, opponentScore, myPacs, enemyPacs, pellets);

            GameState.Debug();

            var gameAI = new GameAI();
            gameAI.ComputeActions();

            var actions = string.Join('|', GameState.myPacs.Values.Select(pac => pac.GetCommand()));

            Player.Debug($"{watch.ElapsedMilliseconds.ToString()} ms");

            Console.WriteLine($"{actions}"); // MOVE <pacId> <x> <y>

        }
    }
}