using System.Collections.Generic;
using UnityEngine;

public static class Dimensions
{
    public struct Box
    {
        public float Height;
        public float Width;
        public float X;
        public float Y;
        public Vector4 TiOff;

        public Box[] DivideByWidth(float[] percents)
        {
            var o = new Box[percents.Length + 1];
            var perUsed = 0f;

            for (var p = 0; p < percents.Length; p++)
            {
                o[p] = new Box
                {
                    Height = Height,
                    Width = percents[p] * Width,
                    X = (2 * perUsed + percents[p] - 1) / 2 * Width + X,
                    Y = Y,
                    TiOff = new Vector4(percents[p] * TiOff.x, TiOff.y, perUsed * TiOff.x + TiOff.z, TiOff.w)
                };
                perUsed += percents[p];
            }

            o[percents.Length] = new Box
            {
                Height = Height,
                Width = (1 - perUsed) * Width,
                X = perUsed / 2 * Width + X,
                Y = Y,
                TiOff = new Vector4((1 - perUsed) * TiOff.x, TiOff.y, perUsed * TiOff.x + TiOff.z, TiOff.w)
            };
            return o;
        }

        public Box[] DivideByHeight(float[] percents)
        {
            var o = new Box[percents.Length + 1];
            var perUsed = 0f;

            for (var p = 0; p < percents.Length; p++)
            {
                o[p] = new Box
                {
                    Height = percents[p] * Height,
                    Width = Width,
                    X = X,
                    Y = (1 - 2 * perUsed - percents[p]) / 2 * Height + Y,
                    TiOff = new Vector4(TiOff.x, percents[p] * TiOff.y, TiOff.z,
                        (1 - perUsed - percents[p]) * TiOff.y + TiOff.w)
                };
                perUsed += percents[p];
            }

            o[percents.Length] = new Box
            {
                Height = (1 - perUsed) * Height,
                Width = Width,
                X = X,
                Y = -perUsed / 2 * Height + Y,
                TiOff = new Vector4(TiOff.x, (1 - perUsed) * TiOff.y, TiOff.z, TiOff.w)
            };
            return o;
        }

        public Box CenterBox(float hPercent, float vPercent)
        {
            return new Box
            {
                Height = vPercent * Height,
                Width = hPercent * Width,
                X = X,
                Y = Y,
                TiOff = new Vector4(
                    hPercent * TiOff.x,
                    vPercent * TiOff.y,
                    (1 - hPercent) / 2 * TiOff.x + TiOff.z,
                    (1 - vPercent) / 2 * TiOff.y + TiOff.w
                )
            };
        }
    }
}