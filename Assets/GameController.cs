using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class GameController : MonoBehaviour
{
    private struct PlaygroundRefs
    {
        public CellDisplay? Score;
        public CellDisplay? BgTop;
        public CellDisplay[] Fg;
        public CellDisplay[] Bg;
        public CellDisplay? BgBottom;
        public CellDisplay? Z;
        public TextMeshPro[] Hint;
    }

    private struct CellDisplay
    {
        public Renderer Renderer;
        public Vector4 TilingOffset;
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
    public GameObject colorsBtnText;
    public GameObject snapBtnText;
    public GameObject hintBtnText;
    public GameObject textureBtnText;
    public Texture2D[] faces;
    private float closeEnough = 0.5f;

    // config
    private const int Notches = 5;
    private const int HoldForToScore = 30;
    private const int RainbowRepeatsPitch = 3;
    private const int RainbowRepeatsRoll = 4;
    private int colorSet = -1;
    private int cellCount = 2;
    private int layout;
    private const int RainbowRepeatsYaw = 5;
    private int score;
    private int heldFor;
    private float[] rawInputs;
    private static readonly int Color1 = Shader.PropertyToID("_Color");
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
    private bool scorePushed;
    private Challenger.Cell[] cells;
    private float snapMode;
    private int hintMode;
    private int textureMode;
    private bool updateLayout = true;
    private const int ShreddedCells = 30;

    public void Start()
    {
        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    public void Update()
    {
        if (!pgRefs.BgTop.HasValue || pgRefs.BgTop.Value.Renderer == null || updateLayout ||
            heldFor >= HoldForToScore ||
            scorePushed)
        {
            if (heldFor >= HoldForToScore || scorePushed)
            {
                score++;
                Handheld.Vibrate();
            }

            if (!pgRefs.BgTop.HasValue || pgRefs.BgTop.Value.Renderer == null || updateLayout)
            {
                updateLayout = false;
                DeleteRenderers();
                BuildMeshes();
                faces = new Texture2D[Notches];
            }

            heldFor = 0;
            scorePushed = false;

            if (cellCount == ShreddedCells)
            {
                cells = Challenger.GetRepeatedChallenge(cellCount, Notches);
            }
            else
            {
                cells = Challenger.GetChallenge(cellCount, Notches, new List<int> { 0, 1, 2, 0, 1, 2 });
            }

            var used = new Dictionary<int, bool>();
            for (var i = 0; i < Notches; i++)
            {
                var start = 74433;
                var end = 75275;
                var r = RandIntn(1 + end - start) + start;
                while (used.ContainsKey(r))
                {
                    r++;
                    if (r >= end)
                    {
                        r = 0;
                    }
                }

                used[r] = true;

                faces[i] = Resources.Load<Texture2D>("Faces/" + r.ToString("000000"));
            }
        }

        for (var i = 0; i < cellCount; i++)
        {
            var target = cells[i].Target;
            var cellColor = Colors.CellColor(target, Notches, colorSets, colorSet);
            var cellTexture = Colors.CellTexture(target, Notches, faces);

            if (i == 0)
            {
                SetRenderProps(pgRefs.BgTop.Value, cellColor, cellTexture, textureMode);
            }

            SetRenderProps(pgRefs.Bg[i], cellColor, cellTexture, textureMode);
            if (i + 1 == cellCount)
            {
                SetRenderProps(pgRefs.BgBottom.Value, cellColor, cellTexture, textureMode);
            }
        }

        if (!Input.gyro.enabled)
        {
            Input.gyro.enabled = true;
        }

        rawInputs = RawInputs.Get(rawInputs, mockGyroscope);

        var inputToRaw = new Dictionary<int, float>();
        var values = new float[cellCount];
        for (var i = 0; i < cellCount; i++)
        {
            var input = cells[i].Input;
            if (!inputToRaw.ContainsKey(input))
            {
                inputToRaw.Add(input, rawInputs[input]);
            }

            values[i] = GetVal(inputToRaw[input], cells[i], Notches, snapMode);
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
            SetRenderProps(pgRefs.Fg[i], Colors.CellColor(values[i], Notches, colorSets, colorSet),
                Colors.CellTexture(values[i], Notches, faces), textureMode);
        }

        SetRenderProps(pgRefs.Z.Value, Color.black, Texture2D.whiteTexture, 1);

        var scoring = true;
        for (var i = 0; i < cellCount; i++)
        {
            if (Mathf.Abs(values[i] - cells[i].Target) > closeEnough &&
                (cells[i].Target != 0 || Mathf.Abs(values[i] - Notches) > closeEnough))
            {
                scoring = false;
                pgRefs.Hint[i].SetText((cells[i].Input % 9) switch
                {
                    0 => "|",
                    1 => "-",
                    2 => "O",
                    3 => "|+",
                    4 => "-+",
                    5 => "O+",
                    6 => "|-",
                    7 => "--",
                    _ => "O-",
                });
                pgRefs.Hint[i].color = hintMode switch
                {
                    0 => Color.black,
                    1 => Color.white,
                    _ => Color.clear
                };
            }
            else
            {
                pgRefs.Hint[i].SetText("");
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

    private static int RandIntn(int n)
    {
        var x = Mathf.FloorToInt(UnityEngine.Random.Range(0, n));
        if (x >= n)
        {
            x = 0;
        }

        return x;
    }

    private void DeleteRenderers()
    {
        var i = 0;
        var children = new GameObject[playground.transform.childCount];

        foreach (Transform child in playground.transform)
        {
            if (!child.name.StartsWith("Fg") &&
                !child.name.StartsWith("Bg") &&
                !child.name.StartsWith("Hint") &&
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
        var photoWidthPercent = 218 / backgroundHeight / 178;

        //     new Vector3(7f / 8, scoreHeight, 1));
        //     new Vector3(0, backgroundHeight / 2 - scoreHeight / 2 - 1f / 5, 0.001f),
        // pr.Score = BuildMesh(trans, "Score", PrimitiveType.Quad,
        pgRefs.BgTop = BuildPrimitive(trans, "Bg Top", PrimitiveType.Quad,
            new Vector3(0, foregroundHeight / 2 + topHeight / 2 - overlap / 2, 0.003f),
            new Vector3(1, topHeight + overlap, 1),
            new Vector4(photoWidthPercent, topHeight / backgroundHeight, (1 - photoWidthPercent) / 2,
                1 - topHeight / backgroundHeight));
        pgRefs.Fg = new CellDisplay[cellCount];
        pgRefs.Hint = new TextMeshPro[cellCount];
        pgRefs.Bg = new CellDisplay[cellCount];

        switch (layout)
        {
            // if (cells == 1)
            // {
            //     BuildMesh(trans, "Fg 01", PrimitiveType.Quad, new Vector3(0, 0, 0.001f),
            //         new Vector3(foregroundWidth, foregroundHeight, 1));
            //     BuildMesh(trans, "Bg 01", PrimitiveType.Quad, new Vector3(0, 0, 0.002f),
            //         new Vector3(1, foregroundHeight, 1));
            // } else 
            case 0:
            {
                var restHeight = 2f * backgroundHeight / 5 - topHeight;
                var oneHeight = foregroundHeight - restHeight;

                pgRefs.Fg[0] = BuildPrimitive(trans, "Fg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, oneHeight, 1),
                    new Vector4(foregroundWidth * photoWidthPercent, oneHeight / backgroundHeight,
                        (1 - foregroundWidth * photoWidthPercent) / 2,
                        1 - topHeight / backgroundHeight - oneHeight / backgroundHeight));
                pgRefs.Hint[0] = BuildTMP(trans, "Hint 01", PrimitiveType.Quad,
                    new Vector3(foregroundWidth / 2 - 1 / 10f, 1 / 10f + restHeight - foregroundHeight / 2, 0),
                    new Vector3(.1f, .14f, .1f));
                pgRefs.Bg[0] = BuildPrimitive(trans, "Bg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight - foregroundHeight / 2, 0.002f),
                    new Vector3(1, oneHeight, 1),
                    new Vector4(photoWidthPercent, oneHeight / backgroundHeight, (1 - photoWidthPercent) / 2,
                        1 - topHeight / backgroundHeight - oneHeight / backgroundHeight));

                pgRefs.Fg[1] = BuildPrimitive(trans, "Fg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, restHeight, 1),
                    new Vector4(foregroundWidth * photoWidthPercent, restHeight / backgroundHeight,
                        (1 - foregroundWidth * photoWidthPercent) / 2, topHeight / backgroundHeight));
                pgRefs.Hint[1] = BuildTMP(trans, "Hint 02", PrimitiveType.Quad,
                    new Vector3(foregroundWidth / 2 - 1 / 10f, 1 / 10f - foregroundHeight / 2, 0),
                    new Vector3(.1f, .14f, .1f));
                pgRefs.Bg[1] = BuildPrimitive(trans, "Bg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1),
                    new Vector4(photoWidthPercent, restHeight / backgroundHeight, (1 - photoWidthPercent) / 2,
                        topHeight / backgroundHeight));
                break;
            }
            case 1:
            {
                var restHeight = 2f * backgroundHeight / 5 - topHeight;
                var oneHeight = foregroundHeight - restHeight;

                pgRefs.Fg[0] = BuildTriangle(trans, "Fg 01",
                    new Vector3(0, oneHeight / 2 + restHeight - foregroundHeight / 2, 0.001f),
                    foregroundWidth * oneHeight / foregroundHeight, oneHeight);
                pgRefs.Hint[0] = BuildTMP(trans, "Hint 01", PrimitiveType.Quad,
                    new Vector3(-10, -10, 0.001f),
                    new Vector3(foregroundWidth, foregroundHeight / ShreddedCells, 1));
                pgRefs.Bg[0] = BuildPrimitive(trans, "Bg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight - foregroundHeight / 2, 0.002f),
                    new Vector3(1, oneHeight, 1), new Vector4(1, 1, 0, 0));

                pgRefs.Fg[1] = BuildTrapezium(trans, "Fg 02",
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.001f),
                    foregroundWidth * oneHeight / foregroundHeight, foregroundWidth, restHeight);
                pgRefs.Hint[1] = BuildTMP(trans, "Hint 02", PrimitiveType.Quad,
                    new Vector3(-10, -10, 0.001f),
                    new Vector3(foregroundWidth, foregroundHeight / ShreddedCells, 1));
                pgRefs.Bg[1] = BuildPrimitive(trans, "Bg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1), new Vector4(1, 1, 0, 0));
                break;
            }
            case 2:
            {
                var oneHeight = backgroundHeight / 3 - topHeight;
                var restHeight = (foregroundHeight - oneHeight) / 2;

                pgRefs.Fg[0] = BuildPrimitive(trans, "Fg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, oneHeight, 1), new Vector4(1, 1, 0, 0));
                pgRefs.Hint[0] = BuildTMP(trans, "Hint 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0),
                    new Vector3(.1f, .14f, 1));
                pgRefs.Bg[0] = BuildPrimitive(trans, "Bg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, oneHeight, 1), new Vector4(1, 1, 0, 0));

                pgRefs.Fg[1] = BuildPrimitive(trans, "Fg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 1.5f - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, restHeight, 1), new Vector4(1, 1, 0, 0));
                pgRefs.Hint[1] = BuildTMP(trans, "Hint 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 1.5f - foregroundHeight / 2, 0),
                    new Vector3(.1f, .14f, 1));
                pgRefs.Bg[1] = BuildPrimitive(trans, "Bg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 1.5f - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1), new Vector4(1, 1, 0, 0));

                pgRefs.Fg[2] = BuildPrimitive(trans, "Fg 03", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 0.5f - foregroundHeight / 2, 0.001f),
                    new Vector3(foregroundWidth, restHeight, 1), new Vector4(1, 1, 0, 0));
                pgRefs.Hint[2] = BuildTMP(trans, "Hint 03", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 0.5f - foregroundHeight / 2, 0),
                    new Vector3(.1f, .14f, 1));
                pgRefs.Bg[2] = BuildPrimitive(trans, "Bg 03", PrimitiveType.Quad,
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1), new Vector4(1, 1, 0, 0));
                break;
            }
            case 3:
            {
                var oneHeight = backgroundHeight / 3 - topHeight;
                var restHeight = (foregroundHeight - oneHeight) / 2;

                pgRefs.Fg[0] = BuildTriangle(trans, "Fg 01",
                    new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0.001f),
                    foregroundWidth * oneHeight / foregroundHeight, oneHeight);
                pgRefs.Hint[0] = BuildTMP(trans, "Hint 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0),
                    new Vector3(.1f, .14f, 1));
                pgRefs.Bg[0] = BuildPrimitive(trans, "Bg 01", PrimitiveType.Quad,
                    new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, oneHeight, 1), new Vector4(1, 1, 0, 0));

                pgRefs.Fg[1] = BuildTrapezium(trans, "Fg 02",
                    new Vector3(0, restHeight * 1.5f - foregroundHeight / 2, 0.001f),
                    foregroundWidth * oneHeight / foregroundHeight,
                    foregroundWidth * (oneHeight + restHeight) / foregroundHeight, restHeight);
                pgRefs.Hint[1] = BuildTMP(trans, "Hint 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 1.5f - foregroundHeight / 2, 0),
                    new Vector3(.1f, .14f, 1));
                pgRefs.Bg[1] = BuildPrimitive(trans, "Bg 02", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 1.5f - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1), new Vector4(1, 1, 0, 0));

                pgRefs.Fg[2] = BuildTrapezium(trans, "Fg 03",
                    new Vector3(0, restHeight * 0.5f - foregroundHeight / 2, 0.001f),
                    foregroundWidth * (oneHeight + restHeight) / foregroundHeight,
                    foregroundWidth, restHeight);
                pgRefs.Hint[2] = BuildTMP(trans, "Hint 03", PrimitiveType.Quad,
                    new Vector3(0, restHeight * 0.5f - foregroundHeight / 2, 0),
                    new Vector3(.1f, .14f, 1));
                pgRefs.Bg[2] = BuildPrimitive(trans, "Bg 03", PrimitiveType.Quad,
                    new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.002f),
                    new Vector3(1, restHeight, 1), new Vector4(1, 1, 0, 0));
                break;
            }
            case 4:
            {
                for (var i = 0; i < ShreddedCells; i++)
                {
                    pgRefs.Fg[i] = BuildPrimitive(trans, "Fg " + (i + 1).ToString("D2"), PrimitiveType.Quad,
                        new Vector3(0, foregroundHeight / 2 - (1 + 2 * i) * foregroundHeight / (2 * ShreddedCells),
                            0.001f),
                        new Vector3(foregroundWidth, foregroundHeight / ShreddedCells, 1), new Vector4(1, 1, 0, 0));
                    pgRefs.Hint[i] = BuildTMP(trans, "Hint " + (i + 1).ToString("D2"), PrimitiveType.Quad,
                        new Vector3(-10, -10, 0.001f),
                        new Vector3(foregroundWidth, foregroundHeight / ShreddedCells, 1));
                    pgRefs.Bg[i] = BuildPrimitive(trans, "Bg " + (i + 1).ToString("D2"), PrimitiveType.Quad,
                        new Vector3(0, foregroundHeight / 2 - (1 + 2 * i) * foregroundHeight / (2 * ShreddedCells),
                            0.002f),
                        new Vector3(1, foregroundHeight / ShreddedCells, 1), new Vector4(1, 1, 0, 0));
                }

                break;
            }
        }

        pgRefs.BgBottom = BuildPrimitive(trans, "Bg Bottom", PrimitiveType.Quad,
            new Vector3(0, -foregroundHeight / 2 - topHeight / 2 + overlap / 2, 0.003f),
            new Vector3(1, topHeight + overlap, 1),
            new Vector4(photoWidthPercent, topHeight / backgroundHeight, (1 - photoWidthPercent) / 2, 0));

        pgRefs.Z = BuildPrimitive(trans, "Z", PrimitiveType.Cube, new Vector3(0, 0, 0.06f),
            new Vector3(1.15f, backgroundHeight + 0.15f, 0.1f), new Vector4(1, 1, 0, 0));
    }

    private static CellDisplay BuildTriangle(Transform trans, string name, Vector3 localPosition, float width,
        float height)
    {
        var go = new GameObject
        {
            name = name,
            transform =
            {
                parent = trans,
                localPosition = localPosition
            }
        };

        var collider = go.GetComponent<Collider>();
        DestroyImmediate(collider);

        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = Resources.Load<Material>("Materials/ThomasMountain");

        var meshFilter = go.AddComponent<MeshFilter>();

        var mesh = new Mesh();

        var vertices = new[]
        {
            new Vector3(-width / 2, -height / 2, 0),
            new Vector3(width / 2, -height / 2, 0),
            new Vector3(0, height / 2, 0)
        };
        mesh.vertices = vertices;

        var tris = new[]
        {
            0, 2, 1,
            2, 1, 0
        };
        mesh.triangles = tris;

        var normals = new[]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        var uv = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(.5f, 1)
        };
        mesh.uv = uv;

        meshFilter.mesh = mesh;
        return new CellDisplay
        {
            Renderer = meshRenderer,
            TilingOffset = new Vector4(1, 1, 0, 0)
        };
    }

    private static CellDisplay BuildTrapezium(Transform trans, string name, Vector3 localPosition, float topWidth,
        float bottomWidth, float height)
    {
        var go = new GameObject
        {
            name = name,
            transform =
            {
                parent = trans,
                localPosition = localPosition
            }
        };

        var collider = go.GetComponent<Collider>();
        DestroyImmediate(collider);

        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = Resources.Load<Material>("Materials/ThomasMountain");

        var meshFilter = go.AddComponent<MeshFilter>();

        var mesh = new Mesh();

        var vertices = new[]
        {
            new Vector3(-bottomWidth / 2, -height / 2, 0),
            new Vector3(bottomWidth / 2, -height / 2, 0),
            new Vector3(-topWidth / 2, height / 2, 0),
            new Vector3(topWidth / 2, height / 2, 0)
        };
        mesh.vertices = vertices;

        var tris = new[]
        {
            0, 2, 1,
            2, 3, 1
        };
        mesh.triangles = tris;

        var normals = new[]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        var uv = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;

        meshFilter.mesh = mesh;
        return new CellDisplay
        {
            Renderer = meshRenderer,
            TilingOffset = new Vector4(1, 1, 0, 0)
        };
    }

    private static CellDisplay BuildPrimitive(Transform trans, string name, PrimitiveType pt,
        Vector3 localPosition,
        Vector3 localScale, Vector4 tilingOffset)
    {
        var go = GameObject.CreatePrimitive(pt);
        go.name = name;
        go.transform.parent = trans;
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        var collider = go.GetComponent<Collider>();
        DestroyImmediate(collider);

        var rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = Resources.Load<Material>("Materials/ThomasMountain");
        return new CellDisplay
        {
            Renderer = rend,
            TilingOffset = tilingOffset
        };
    }

    private static TextMeshPro BuildTMP(Transform trans, string name, PrimitiveType pt,
        Vector3 localPosition, Vector3 localScale)
    {
        var go = new GameObject
        {
            name = name,
            transform =
            {
                parent = trans,
                localPosition = localPosition,
                localScale = localScale
            }
        };

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/CherryCreamSoda-Regular SDF");
        tmp.UpdateFontAsset();
        tmp.fontSize = 8;
        tmp.alignment = TextAlignmentOptions.Center;

        tmp.autoSizeTextContainer = true;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(.73f, 1.02f);

        return tmp;
    }

    private static void SetRenderProps(CellDisplay cd, Color color, Texture2D t, int textureMode)
    {
        var propBlock = new MaterialPropertyBlock();
        if (textureMode == 0)
        {
            propBlock.SetColor(Color1, Color.white);
            propBlock.SetTexture(MainTex, t);
            propBlock.SetVector(MainTexSt, cd.TilingOffset);
        }
        else
        {
            propBlock.SetColor(Color1, color);
            propBlock.SetTexture(MainTex, Texture2D.whiteTexture);
            propBlock.SetVector(MainTexSt, new Vector4(1, 1, 0, 0));
        }

        cd.Renderer.SetPropertyBlock(propBlock);
    }

    private static float GetVal(float rawVal, Challenger.Cell cell, int notches, float snapMode)
    {
        var rainbowRepeats = (cell.Input % 3) switch
        {
            0 => RainbowRepeatsPitch,
            1 => RainbowRepeatsRoll,
            _ => RainbowRepeatsYaw
        };

        float val;
        if (cell.Forward)
        {
            val = rawVal * rainbowRepeats * notches + cell.Start;
        }
        else
        {
            val = (4 + (1 - rawVal) * rainbowRepeats) * notches - 3 * cell.Target - cell.Start;
        }

        if (Math.Abs(snapMode - 1f) < .001f)
        {
            val = Mathf.Floor(val + .5f);
        }
        else if (Math.Abs(snapMode - .5f) < .001f)
        {
            val = Mathf.Floor(val * 2 + .5f) / 2;
        }

        return val % notches;
    }

    public void OnColorsButtonPress()
    {
        if (colorSet == -1)
        {
            colorSet = 0;
        }
        else
        {
            colorSet++;
            if (colorSet >= colorSets.Length)
            {
                colorSet = -1;
            }
        }

        var tmp = colorsBtnText.GetComponentInChildren<TextMeshProUGUI>();
        tmp.SetText("COLORS " + (colorSet + 2));
    }

    public void OnLayoutButtonPress()
    {
        layout++;
        if (layout >= 5)
        {
            layout = 0;
        }

        cellCount = layout switch
        {
            0 => 2,
            1 => 2,
            2 => 3,
            3 => 3,
            _ => ShreddedCells,
        };
        updateLayout = true;
    }

    public void OnScoreButtonPress()
    {
        scorePushed = true;
    }

    public void OnHintButtonPress()
    {
        var tmp = hintBtnText.GetComponentInChildren<TextMeshProUGUI>();
        switch (hintMode)
        {
            case 0:
                hintMode = 1;
                tmp.SetText("HINT W");
                break;
            case 1:
                hintMode = 2;
                tmp.SetText("HINT OFF");
                break;
            default:
                hintMode = 0;
                tmp.SetText("HINT B");
                break;
        }
    }

    public void OnTextureButtonPress()
    {
        var tmp = textureBtnText.GetComponentInChildren<TextMeshProUGUI>();
        if (textureMode == 0)
        {
            textureMode = 1;
            closeEnough = .2f;
            tmp.SetText("TEXTURE OFF");
        }
        else
        {
            textureMode = 0;
            closeEnough = .5f;
            tmp.SetText("TEXTURE ON");
        }
    }

    public void OnSnapButtonPress()
    {
        var tmp = snapBtnText.GetComponentInChildren<TextMeshProUGUI>();
        if (snapMode == 0)
        {
            snapMode = .5f;
            tmp.SetText("ASSIST");
        }
        else if (Math.Abs(snapMode - .5f) < .001f)
        {
            snapMode = 1f;
            tmp.SetText("SNAP ON");
        }
        else
        {
            snapMode = 0;
            tmp.SetText("SNAP OFF");
        }
    }
}