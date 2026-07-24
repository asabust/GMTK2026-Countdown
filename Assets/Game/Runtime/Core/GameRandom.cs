using System;
using UnityEngine;

public static class GameRandom
{
    private static System.Random seededRandom;

    public static bool UsesFixedSeed => seededRandom != null;

    public static void SetFixedSeed(int seed)
    {
        seededRandom = new System.Random(seed);
    }

    public static void ClearFixedSeed()
    {
        seededRandom = null;
    }

    public static int RangeInclusive(int minimum, int maximum)
    {
        if (maximum < minimum)
        {
            (minimum, maximum) = (maximum, minimum);
        }

        return seededRandom != null
            ? seededRandom.Next(minimum, maximum + 1)
            : UnityEngine.Random.Range(minimum, maximum + 1);
    }

    public static bool Chance(float probability)
    {
        float clamped = Mathf.Clamp01(probability);
        double value = seededRandom != null
            ? seededRandom.NextDouble()
            : UnityEngine.Random.value;
        return value < clamped;
    }
}
