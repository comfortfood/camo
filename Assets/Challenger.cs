using System.Collections.Generic;
using UnityEngine;

public static class Challenger
{
    public struct Cell
    {
        public int Input;
        public int Target;
        public float Start;
        public bool Forward;
    }

    public static Cell[] GetChallenge(int cellCount, int notches, List<int> urn)
    {
        var cells = new Cell[cellCount];

        var lowOffsets = new Dictionary<int, int>();
        for (var s = 0; s < cellCount; s++)
        {
            var u = RandIntn(urn.Count);
            var input = urn[u];
            urn.RemoveAt(u);

            var target = RandIntn(notches);

            var prevLowOffset = lowOffsets.GetValueOrDefault(input % 9, 0);
            var (start, offset) = StartAndLowOffset(prevLowOffset, target, input, notches);
            lowOffsets[input % 9] = offset;

            var cell = new Cell
            {
                Input = input,
                Target = target,
                Start = start,
                Forward = RandIntn(2) == 0
            };
            cells[s] = cell;
        }

        return cells;
    }

    public static Cell[] GetRepeatedChallenge(int cellCount, int notches)
    {
        var targets = new List<int>();
        for (var n = 0; n < notches; n++)
        {
            targets.Add(n);
        }

        var inputTypes = new List<int> { 0, 1, 2 };

        Shuffle(targets);
        Shuffle(inputTypes);

        var inputs = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            inputs.Add(inputTypes[i % 2]);
        }

        var cells = new Cell[cellCount];
        // for (var i = 0; i < inputTypes.Count; i++)
        // {
        //     var target = targets[i % inputTypes.Count];
        //     cells[i] = new Cell
        //     {
        //         Input = inputTypes[i % inputTypes.Count],
        //         Target = target,
        //         Start = RandIntnNotX(notches, target, 1),
        //     };
        // }

        var randCells = GetChallenge(3, notches, inputs);
        for (var r = 0; r < randCells.Length; r++)
        {
            cells[r] = randCells[r];
        }

        for (var c = randCells.Length; c < cellCount; c++)
        {
            cells[c] = cells[c % randCells.Length];
        }

        return cells;
    }

    private static void Shuffle<T>(IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = Random.Range(i, count);
            (ts[i], ts[r]) = (ts[r], ts[i]);
        }
    }

    private static (float, int) StartAndLowOffset(int lowOffset, int target, int input, int notches)
    {
        float start;
        if (lowOffset == 0 && input >= 9)
        {
            start = RandIntnNotX(2 * notches, target, .5f);
            return (start, (int)(2 * (start + notches - target)) % notches);
        }

        if (lowOffset == 0)
        {
            start = RandIntnNotX(notches, target, 1);
            return (start, ((int)start + notches - target) % notches);
        }

        start = lowOffset;
        if (input >= 9)
        {
            if (RandIntn(2) == 0)
            {
                start += notches;
            }

            start *= .5f;
        }

        return ((int)(start + target) % notches, lowOffset);
    }

    private static int RandIntn(int n)
    {
        var x = Mathf.FloorToInt(Random.Range(0, n));
        if (x >= n)
        {
            x = 0;
        }

        return x;
    }

    private static float RandIntnNotX(int n, int x, float increment)
    {
        var y = RandIntn(n - 1) * increment;
        if (y >= x)
        {
            y += increment;
        }

        return y % n;
    }
}