using UnityEngine;

public interface IRando
{
    public int RandIntn(int n);
    public float RandIntnNotX(int n, int x, float increment);
}

public class Rando : IRando
{
    public int RandIntn(int n)
    {
        var x = Mathf.FloorToInt(Random.Range(0, n));
        if (x >= n)
        {
            x = 0;
        }

        return x;
    }

    public float RandIntnNotX(int n, int x, float increment)
    {
        var y = RandIntn(n - 1) * increment;
        if (y >= x)
        {
            y += increment;
        }

        return y % n;
    }
}