using System;
using UnityEngine;

public static class Colors
{
    public static Color CellColor(float val, int notches, GameController.ColorSet[] colorSets, int colorSet)
    {
        if (colorSet == -1)
        {
            return Color.HSVToRGB(val % notches / notches, 1, 1);
        }

        var floor = (int)Math.Floor(val);
        var ceiling = (floor + 1) % notches;
        var fColor = colorSets[colorSet].colors[floor];
        var cColor = colorSets[colorSet].colors[ceiling];
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

    public static Texture2D CellTexture(float val, int notches, Texture2D[] textures)
    {
        return textures[(int)Mathf.Floor(val + .5f) % notches];
    }
}