﻿using System;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

public class GameAI
{
    public void ComputeActions()
    {
        var myPacs = GameState.myPacs.ToList();
        var random = new Random();

        foreach (var kvp in myPacs)
        {
            var pacId = kvp.Key;
            var pac = kvp.Value;

            if (pac.abilityCooldown == 0)
            {
                pac.ActivateSpeed();
                continue;
            }
            
            if(pac.HasMove == false)
            {
                pac.Move();
            } 
        }
    }

}
