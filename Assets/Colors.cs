using System;
using UnityEngine;

public static class Colors
{
    public static Color CellColor(float val, int notches, GameController.ColorSet[] colorSets, int colorSet)
    {
        if (colorSet == -1)
        {
            return Color.HSVToRGB(val / notches, 1, 1);
        }

        var fColor = colorSets[colorSet].colors[(int)Math.Floor(val)];
        var cColor = colorSets[colorSet].colors[(int)(Math.Ceiling(val) % notches)];
        var f = 1 - val % 1f;
        var c = val % 1f;
        return new Color
        {
            a = 1,
            r = fColor.r * f + cColor.r * c,
            b = fColor.b * f + cColor.b * c,
            g = fColor.g * f + cColor.g * c,
        };
    }
}