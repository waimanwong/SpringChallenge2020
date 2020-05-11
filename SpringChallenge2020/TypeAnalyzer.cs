using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class TypeAnalyzer
{
    public const string ROCK = "ROCK";
    public const string PAPER = "PAPER";
    public const string SCISSORS = "SCISSORS";

    public static bool TryGetDominantType(List<string> types, out string dominantType)
    {
        dominantType = string.Empty;

        var distinctTypes = types.Distinct().ToList();

        if(distinctTypes.Count == 2)
        {
            dominantType = Compare(distinctTypes[0], distinctTypes[1]) > 0 ?
                distinctTypes[0] :
                distinctTypes[1];
        }

        return string.IsNullOrEmpty(dominantType) == false;
    }

    /// <summary>
    /// return signednumber: myType - enemyType
    /// </summary>
    /// <param name="myType"></param>
    /// <param name="enemyType"></param>
    /// <returns></returns>
    public static int Compare(string myType, string enemyType)
    {   
        switch (myType)
        {
            case ROCK:
                switch (enemyType)
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
