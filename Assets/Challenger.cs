using System.Collections.Generic;
using UnityEngine;

public class Challenger
{
    private readonly IRando rando;

    public Challenger(IRando rando)
    {
        this.rando = rando;
    }

    public struct Cell
    {
        public int Input;
        public float InputOffset;
        public float Start;
        public int DisplayOffset;
        public bool Forward;
        public int HeldFor;
        public float HeldOnValue;
    }

    public Cell[] GetChallenge(int cellCount, int notches, List<int> urn)
    {
        var cells = new Cell[cellCount];

        var lowInputToStart = new Dictionary<int, int>();
        for (var s = 0; s < cellCount; s++)
        {
            var u = rando.RandIntn(urn.Count);
            var input = urn[u];
            urn.RemoveAt(u);

            var start = lowInputToStart.GetValueOrDefault(input % 9, -1);
            if (start == -1)
            {
                start = rando.RandIntn(notches);
                lowInputToStart[input % 9] = start;
            }

            if (input > 9)
            {
                start /= 2;
                if (rando.RandIntn(2) == 0)
                {
                    start = (start + notches / 2) % notches;
                }
            }

            var cell = new Cell
            {
                Input = input,
                Start = start,
                DisplayOffset = rando.RandIntn(notches),
                Forward = rando.RandIntn(2) == 0
            };

            cells[s] = cell;
        }

        return cells;
    }

    public Cell[] GetRepeatedChallenge(int cellCount, int notches)
    {
        var inputTypes = new List<int> { 0, 1, 2 };

        Shuffle(inputTypes);

        var inputs = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            inputs.Add(inputTypes[i % 2]);
        }

        var randCells = GetChallenge(3, notches, inputs);

        var cells = new Cell[cellCount];
        for (var c = 0; c < cellCount; c++)
        {
            cells[c] = randCells[c % randCells.Length];
        }

        return cells;
    }

    private void Shuffle<T>(IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = Random.Range(i, count);
            (ts[i], ts[r]) = (ts[r], ts[i]);
        }
    }
}