using UnityEngine;

public static class Colors
{ 
    public static Color CellColor(float val, int notches, Color[] colors)
    {
        val %= notches;
        return Color.HSVToRGB(val / notches, 1, 1);
    }
}