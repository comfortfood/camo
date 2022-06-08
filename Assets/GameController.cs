using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GameController : MonoBehaviour
{
    private struct PlaygroundRefs
    {
        public Renderer Score;
        public Renderer BgTop;
        public Renderer[] Fg;
        public Renderer[] Bg;
        public Renderer BgBottom;
        public Renderer Z;
    }

    [Serializable]
    public struct ColorSet
    {
        public ColorSet(Color[] colors)
        {
            this.colors = colors;
        }

        public Color[] colors;
    }

    public ColorSet[] colorSets =
    {
        new(new[]
        {
            Color.cyan
        })
    };

    public GameObject playground;
    private PlaygroundRefs pgRefs;
    public GameObject mockGyroscope;

    // config
    private int colorSet;
    private int cellCount = ShreddedCells;
    private const int Notches = 5;
    private const int InputTypes = 3;
    private const int HoldForToScore = 30;
    private const float CloseEnough = 0.2f;
    private const int RainbowRepeatsPitch = 3;
    private const int RainbowRepeatsRoll = 4;
    private const int RainbowRepeatsYaw = 5;
    private int score;
    private int heldFor;
    private float[] rawInputs;
    private static readonly int Color1 = Shader.PropertyToID("_Color");
    private bool scorePushed;
    private Challenger.Cell[] cells;
    private const int ShreddedCells = 60;

    public void Start()
    {
        Input.gyro.enabled = true;
        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    public void Update()
    {
        if (pgRefs.BgTop == null || pgRefs.Bg.Length != cellCount || heldFor >= HoldForToScore || scorePushed)
        {
            if (heldFor >= HoldForToScore || scorePushed)
            {
                score++;
                Handheld.Vibrate();
            }

            if (pgRefs.BgTop == null || pgRefs.Bg.Length != cellCount)
            {
                DeleteRenderers();
                BuildMeshes();
            }

            heldFor = 0;
            scorePushed = false;

            if (cellCount == ShreddedCells)
            {
                cells = Challenger.GetRepeatedChallenge(cellCount, Notches);
            }
            else
            {
                cells = Challenger.GetChallenge(cellCount, Notches, new List<int> { 0, 1, 2 });
            }

            for (var i = 0; i < cellCount; i++)
            {
                if (i == 0)
                {
                    SetMeshColor(pgRefs.BgTop, Colors.CellColor(cells[i].Target, Notches, colorSets[colorSet].colors));
                }

                SetMeshColor(pgRefs.Bg[i], Colors.CellColor(cells[i].Target, Notches, colorSets[colorSet].colors));
                if (i + 1 == cellCount)
                {
                    SetMeshColor(pgRefs.BgBottom,
                        Colors.CellColor(cells[i].Target, Notches, colorSets[colorSet].colors));
                }
            }
        }

        rawInputs = RawInputs.Get(rawInputs, mockGyroscope);
        var values = new float[cellCount];
        for (var i = 0; i < cellCount; i++)
        {
            values[i] = GetVal(rawInputs, cells[i], Notches);
        }

        // var childTrans = trans.Find("Text 01");
        // var tmp = childTrans.GetComponent<TextMeshPro>();
        // tmp.color = colorSets[colorSet].colors[0];
        //
        // childTrans = trans.Find("Text 02");
        // tmp = childTrans.GetComponent<TextMeshPro>();
        // tmp.color = colorSets[colorSet].colors[0];

        for (var i = 0; i < cellCount; i++)
        {
            SetMeshColor(pgRefs.Fg[i], Colors.CellColor(values[i], Notches, colorSets[colorSet].colors));
        }

        // SetMeshColor(pr.Score, colorSets[colorSet].colors[1]);

        SetMeshColor(pgRefs.Z, Color.black);

        var scoring = true;
        for (var i = 0; i < cellCount; i++)
        {
            if (Mathf.Abs(values[i] - cells[i].Target) > CloseEnough &&
                (cells[i].Target != 0 || Mathf.Abs(values[i] - Notches) > CloseEnough))
            {
                scoring = false;
            }
        }

        if (scoring)
        {
            heldFor++;
        }
        else
        {
            heldFor = 0;
        }
    }

    private void DeleteRenderers()
    {
        var i = 0;
        var children = new GameObject[playground.transform.childCount];

        foreach (Transform child in playground.transform)
        {
            if (!child.name.StartsWith("Fg") && !child.name.StartsWith("Bg") &&
                child.name is not ("Score" or "Z")) continue;
            children[i] = child.gameObject;
            i += 1;
        }

        foreach (var child in children)
        {
            if (child != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void BuildMeshes()
    {
        const float goldenRatio = 1.618f;
        const float overlap = 0.01f;
        const float foregroundWidth = 3f / 4;
        const float foregroundHeight = foregroundWidth * goldenRatio;

        var displayWidth = Screen.width;
        var displayHeight = Screen.height;
        var backgroundHeight = 1f / displayWidth * displayHeight;
        var topHeight = (backgroundHeight - foregroundHeight) / 2;
        var scoreHeight = topHeight / 5;
        var trans = playground.transform;

        //     new Vector3(7f / 8, scoreHeight, 1));
        //     new Vector3(0, backgroundHeight / 2 - scoreHeight / 2 - 1f / 5, 0.001f),
        // pr.Score = BuildMesh(trans, "Score", PrimitiveType.Quad,
        pgRefs.BgTop = BuildMesh(trans, "Bg Top", PrimitiveType.Quad,
            new Vector3(0, foregroundHeight / 2 + topHeight / 2 - overlap / 2, 0.003f),
            new Vector3(1, topHeight + overlap, 1));
        pgRefs.Fg = new Renderer[cellCount];
        pgRefs.Bg = new Renderer[cellCount];

        switch (cellCount)
        {
            // if (cells == 1)
            // {
            //     BuildMesh(trans, "Fg 01", PrimitiveType.Quad, new Vector3(0, 0, 0.001f),
            //         new Vector3(foregroundWidth, foregroundHeight, 1));
            //     BuildMesh(trans, "Bg 01", PrimitiveType.Quad, new Vector3(0, 0, 0.002f),
            //         new Vector3(1, foregroundHeight, 1));
            // } else 
            case 2:
            {
                var restHeight = 2f * backgroundHeight / 5 - topHeight;
                var oneHeight = foregroundHeight - restHeight;
                pgRefs.Fg[0] = BuildMesh(trans, "Fg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, oneHeight, 1));
                pgRefs.Bg[0] = BuildMesh(trans, "Bg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight - foregroundHeight / 2, 0.002f),
                    new Vector3(1, oneHeight, 1));
                pgRefs.Fg[1] = BuildMesh(trans, "Fg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, restHeight, 1));
                pgRefs.Bg[1] = BuildMesh(trans, "Bg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1));
                break;
            }
            case 3:
            {
                var oneHeight = backgroundHeight / 3 - topHeight;
                var restHeight = (foregroundHeight - oneHeight) / 2;
                pgRefs.Fg[0] = BuildMesh(trans, "Fg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, oneHeight, 1));
                pgRefs.Bg[0] = BuildMesh(trans, "Bg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, oneHeight, 1));
                pgRefs.Fg[1] = BuildMesh(trans, "Fg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 1.5f - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, restHeight, 1));
                pgRefs.Bg[1] = BuildMesh(trans, "Bg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 1.5f - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1));
                pgRefs.Fg[2] = BuildMesh(trans, "Fg 03", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 0.5f - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, restHeight, 1));
                pgRefs.Bg[2] = BuildMesh(trans, "Bg 03", PrimitiveType.Quad,
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1));
                break;
            }
            case ShreddedCells:
            {
                for (var i = 0; i < ShreddedCells; i++)
                {
                    pgRefs.Fg[i] = BuildMesh(trans, "Fg " + (i + 1).ToString("D2"), PrimitiveType.Quad,
                        new Vector3(0, foregroundHeight / 2 - (1 + 2 * i) * foregroundHeight / (2 * ShreddedCells),
                            0.001f),
                        new Vector3(foregroundWidth, foregroundHeight / ShreddedCells, 1));
                    pgRefs.Bg[i] = BuildMesh(trans, "Bg " + (i + 1).ToString("D2"), PrimitiveType.Quad,
                        new Vector3(0, foregroundHeight / 2 - (1 + 2 * i) * foregroundHeight / (2 * ShreddedCells),
                            0.002f),
                        new Vector3(1, foregroundHeight / ShreddedCells, 1));
                }

                break;
            }
        }
        // else if (cells == 4)
        // {
        //     float oneHeight = backgroundHeight / 3 - topHeight;
        //     float restHeight = (foregroundHeight - oneHeight) / 4;
        //     BuildMesh(trans, "Fg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight * 4 - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, oneHeight, 1));
        //     BuildMesh(trans, "Fg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 3.5f - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     BuildMesh(trans, "Fg 03", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 2.5f - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     BuildMesh(trans, "Fg 04", PrimitiveType.Quad,
        //         new Vector3(0, restHeight - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, restHeight * 2f, 1));
        //     BuildMesh(trans, "Bg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight * 4 - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, oneHeight, 1));
        //     BuildMesh(trans, "Bg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 3.5f - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, restHeight, 1));
        //     BuildMesh(trans, "Bg 03", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 2.5f - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, restHeight, 1));
        //     BuildMesh(trans, "Bg 04", PrimitiveType.Quad,
        //         new Vector3(0, restHeight - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, restHeight * 2f, 1));
        // }
        // else if (cells == 5)
        // {
        //     float oneHeight = backgroundHeight / 3 - topHeight;
        //     float restHeight = (foregroundHeight - oneHeight) / 5;
        //     BuildMesh(trans, "Fg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight * 5 - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, oneHeight, 1));
        //     BuildMesh(trans, "Fg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 4.5f - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     BuildMesh(trans, "Fg 03", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 3.5f - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     BuildMesh(trans, "Fg 04", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 2.5f - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     BuildMesh(trans, "Fg 05", PrimitiveType.Quad,
        //         new Vector3(0, restHeight - foregroundHeight / 2, 0.001f),
        //         new Vector3(foregroundWidth, restHeight * 2, 1));
        //     BuildMesh(trans, "Bg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight * 5 - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, oneHeight, 1));
        //     BuildMesh(trans, "Bg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 4.5f - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, restHeight, 1));
        //     BuildMesh(trans, "Bg 03", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 3.5f - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, restHeight, 1));
        //     BuildMesh(trans, "Bg 04", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 2.5f - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, restHeight, 1));
        //     BuildMesh(trans, "Bg 05", PrimitiveType.Quad,
        //         new Vector3(0, restHeight - foregroundHeight / 2, 0.002f),
        //         new Vector3(1, restHeight * 2, 1));
        // }

        pgRefs.BgBottom = BuildMesh(trans, "Bg Bottom", PrimitiveType.Quad,
            new Vector3(0, -foregroundHeight / 2 - topHeight / 2 + overlap / 2, 0.003f),
            new Vector3(1, topHeight + overlap, 1));

        pgRefs.Z = BuildMesh(trans, "Z", PrimitiveType.Cube, new Vector3(0, 0, 0.06f),
            new Vector3(1.15f, backgroundHeight + 0.15f, 0.1f));
    }

    private static Renderer BuildMesh(Transform trans, string name, PrimitiveType pt,
        Vector3 localPosition,
        Vector3 localScale)
    {
        var go = GameObject.CreatePrimitive(pt);
        go.name = name;
        go.transform.parent = trans;
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        var collider = go.GetComponent<Collider>();
        DestroyImmediate(collider);

        var rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = Resources.Load("Materials/ThomasMountain") as Material;
        return rend;
    }

    private static void SetMeshColor(Renderer r, Color color)
    {
        var propBlock = new MaterialPropertyBlock();
        propBlock.SetColor(Color1, color);
        r.SetPropertyBlock(propBlock);
    }

    private static float GetVal(IReadOnlyList<float> inputs, Challenger.Cell cell, int notches)
    {
        var rainbowRepeats = (cell.Input % 3) switch
        {
            0 => RainbowRepeatsPitch,
            1 => RainbowRepeatsRoll,
            _ => RainbowRepeatsYaw
        };
        return (inputs[cell.Input] * rainbowRepeats % 1f * notches + cell.Start) % notches;
    }

    public void OnScoreButtonPress()
    {
        scorePushed = true;
    }

    public void OnLayoutButtonPress()
    {
        cellCount = cellCount switch
        {
            2 => 3,
            3 => ShreddedCells,
            _ => 2
        };
    }
}