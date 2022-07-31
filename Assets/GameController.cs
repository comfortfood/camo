using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class GameController : MonoBehaviour
{
    [Serializable]
    public struct Inventory
    {
        public List<InventorySet> sets;
    }

    [Serializable]
    public struct InventorySet
    {
        public string name;
        public List<string> textures;
    }

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

    private static readonly IRando Rando = new Rando();

    public GameObject playground;
    private PlaygroundRefs pgRefs;
    public GameObject mockGyroscope;
    public GameObject colorsBtnText;
    public GameObject snapBtnText;
    public GameObject difficultyBtnText;
    public GameObject hintBtnText;
    public GameObject textureBtnText;
    public Texture2D[] textures;
    private float closeEnough = 0.2f;
    private readonly Challenger challenger = new(Rando);

    // config
    private const int Notches = 5;
    private const int HoldForToScore = 30;
    private const int RainbowRepeatsPitch = 3;
    private const int RainbowRepeatsRoll = 5;
    private const int RainbowRepeatsYaw = 5;
    private const int ShreddedCells = 30;
    private int colorSet;
    private int cellCount;
    private int layout;
    private int score;
    private int heldFor;
    private float[] rawValues;
    private static readonly int Color1 = Shader.PropertyToID("_Color");
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int MainTexSt = Shader.PropertyToID("_MainTex_ST");
    private bool scorePushed;
    private Challenger.Cell[] cells;
    private float snapMode;
    private int hintMode;
    private int difficultyMode;
    private int textureSet;
    private bool updateLayout;
    private Inventory inventory;

    public void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep; // Disable screen dimming

        var inventoryRaw = Resources.Load<TextAsset>("Textures/Inventory");
        inventory = JsonUtility.FromJson<Inventory>(inventoryRaw.text);
        Resources.UnloadAsset(inventoryRaw);

        layout = 0;
        hintMode = 0;
        colorSet = -1;
        textureSet = -1;
        difficultyMode = 1;
        Adapt();
    }

    public void OnColorsButtonPress()
    {
        colorSet++;
        if (colorSet >= colorSets.Length)
        {
            colorSet = -1;
        }

        var tmp = colorsBtnText.GetComponentInChildren<TextMeshProUGUI>();
        tmp.SetText("COLORS " + (colorSet + 2));
    }

    private void Adapt()
    {
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

        if (hintMode >= 3)
        {
            hintMode = 0;
        }

        var tmp = hintBtnText.GetComponentInChildren<TextMeshProUGUI>();
        switch (hintMode)
        {
            case 0:
                tmp.SetText("HINT B");
                break;
            case 1:
                tmp.SetText("HINT W");
                break;
            default:
                tmp.SetText("HINT OFF");
                break;
        }

        if (textureSet >= inventory.sets.Count)
        {
            textureSet = -1;
        }

        closeEnough = textureSet == -1 ? .2f : .5f;

        tmp = textureBtnText.GetComponentInChildren<TextMeshProUGUI>();
        var textureName = textureSet == -1 ? "OFF" : inventory.sets[textureSet].name.ToUpper();
        tmp.SetText("TEXTURE\n" + textureName);

        if (snapMode >= 1.4f)
        {
            snapMode = 0;
        }

        tmp = snapBtnText.GetComponentInChildren<TextMeshProUGUI>();
        if (snapMode == 0)
        {
            tmp.SetText("SNAP OFF");
        }
        else if (Math.Abs(snapMode - .5f) < .001f)
        {
            tmp.SetText("SNAP ASSIST");
        }
        else
        {
            tmp.SetText("SNAP ON");
        }

        if (difficultyMode >= 3)
        {
            difficultyMode = 0;
        }

        tmp = difficultyBtnText.GetComponentInChildren<TextMeshProUGUI>();
        if (difficultyMode == 0)
        {
            tmp.SetText("HOBBY LOBBY");
        }
        else if (difficultyMode == 1)
        {
            tmp.SetText("SOLID CAREER");
        }
        else
        {
            tmp.SetText("ACCLAIMED");
        }
    }

    public void OnLayoutButtonPress()
    {
        layout++;
        Adapt();
        updateLayout = true;
    }

    public void OnScoreButtonPress()
    {
        scorePushed = true;
    }

    public void OnHintButtonPress()
    {
        hintMode++;
        Adapt();
    }

    public void OnTextureButtonPress()
    {
        textureSet++;
        Adapt();
        updateLayout = true;
    }

    public void OnSnapButtonPress()
    {
        snapMode += .5f;
        Adapt();
    }

    public void OnDifficultyButtonPress()
    {
        difficultyMode++;
        Adapt();
    }

    public void Update()
    {
        var setInputOffset = false;

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
                Clean();
                BuildMeshes();
            }

            heldFor = 0;
            scorePushed = false;
            setInputOffset = true;

            if (cellCount == ShreddedCells)
            {
                cells = challenger.GetRepeatedChallenge(cellCount, Notches);
            }
            else
            {
                cells = challenger.GetChallenge(cellCount, Notches, new List<int> { 0, 1, 2, 0, 1, 2 });
            }

            for (var i = 0; i < Notches; i++)
            {
                Resources.UnloadAsset(textures[i]);
            }

            var used = new Dictionary<int, bool>();
            for (var i = 0; i < Notches; i++)
            {
                if (textureSet == -1)
                {
                    continue;
                }

                var r = Rando.RandIntn(inventory.sets[textureSet].textures.Count);
                while (used.ContainsKey(r))
                {
                    r++;
                    if (r >= inventory.sets[textureSet].textures.Count)
                    {
                        r = 0;
                    }
                }

                used[r] = true;

                textures[i] =
                    Resources.Load<Texture2D>(
                        "Textures/" + inventory.sets[textureSet].name + "/" + inventory.sets[textureSet].textures[r]);
            }
        }

        for (var i = 0; i < cellCount; i++)
        {
            var cellColor = Colors.CellColor(cells[i].DisplayOffset, Notches, colorSets, colorSet);
            var cellTexture = Colors.CellTexture(cells[i].DisplayOffset, Notches, textures);

            if (i == 0)
            {
                SetRenderProps(pgRefs.BgTop.Value, cellColor, cellTexture, textureSet);
            }

            SetRenderProps(pgRefs.Bg[i], cellColor, cellTexture, textureSet);
            if (i + 1 == cellCount)
            {
                SetRenderProps(pgRefs.BgBottom.Value, cellColor, cellTexture, textureSet);
            }
        }

        if (!Input.gyro.enabled)
        {
            Input.gyro.enabled = true;
        }

        var values = GetValues(setInputOffset);

        // var childTrans = trans.Find("Text 01");
        // var tmp = childTrans.GetComponent<TextMeshPro>();
        // tmp.color = colorSets[colorSet].colors[0];
        //
        // childTrans = trans.Find("Text 02");
        // tmp = childTrans.GetComponent<TextMeshPro>();
        // tmp.color = colorSets[colorSet].colors[0];

        for (var i = 0; i < cellCount; i++)
        {
            var displayVal = (cells[i].DisplayOffset + values[i]) % Notches;
            var cellColor = Colors.CellColor(displayVal, Notches, colorSets, colorSet);
            var cellTexture = Colors.CellTexture(displayVal, Notches, textures);
            SetRenderProps(pgRefs.Fg[i], cellColor, cellTexture, textureSet);
        }

        SetRenderProps(pgRefs.Z.Value, Color.black, Texture2D.whiteTexture, -1);

        var scoring = true;
        for (var i = 0; i < cellCount; i++)
        {
            if (values[i] > closeEnough && Notches - values[i] > closeEnough && cells[i].HeldFor < HoldForToScore)
            {
                scoring = false;
                cells[i].HeldFor = 0;
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
                if (cells[i].HeldFor >= HoldForToScore)
                {
                    pgRefs.Hint[i].SetText("L");
                }
                else
                {
                    if (difficultyMode == 0)
                    {
                        cells[i].HeldOnValue = values[i];
                        cells[i].HeldFor++;
                    }
                    pgRefs.Hint[i].SetText("");
                }
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

    private void Clean()
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

        if (textures != null)
        {
            foreach (var t in textures)
            {
                Resources.UnloadAsset(t);
            }
        }

        textures = new Texture2D[Notches];
    }

    private void BuildMeshes()
    {
        var displayWidth = Screen.width;
        var displayHeight = Screen.height;
        var background = new Dimensions.Box
        {
            Height = 1f / displayWidth * displayHeight,
            Width = 1,
            X = 0,
            Y = 0,
            TiOff = new Vector4(1, 1, 0, 0)
        };
        const float goldenRatio = 1.618f;
        var backgroundRows = background.DivideByHeight(new[]
        {
            (1 - .75f * goldenRatio / background.Height) / 2,
            .75f * goldenRatio / background.Height
        });

        var trans = playground.transform;

        pgRefs.Z = BuildPrimitive(trans, "Z", PrimitiveType.Cube, new Dimensions.Box
        {
            Height = background.Height + .15f,
            Width = 1.15f
        }, 0.06f);

        pgRefs.BgTop = BuildPrimitive(trans, "Bg Top", PrimitiveType.Quad, backgroundRows[0], .003f);
        pgRefs.BgBottom = BuildPrimitive(trans, "Bg Bottom", PrimitiveType.Quad, backgroundRows[2], 0.003f);

        pgRefs.Bg = new CellDisplay[cellCount];
        pgRefs.Fg = new CellDisplay[cellCount];
        pgRefs.Hint = new TextMeshPro[cellCount];
        Dimensions.Box[] stripes;
        switch (layout)
        {
            case 0:
            case 1:
                stripes = backgroundRows[1].DivideByHeight(new[] { 2 / 3f });
                break;
            case 2:
            case 3:
                stripes = backgroundRows[1].DivideByHeight(new[] { 1f / 4, 1.5f / 4, 1.5f / 4 });
                break;
            default:
                stripes = backgroundRows[1].DivideByHeight(Enumerable.Repeat(1f / cellCount, cellCount).ToArray());
                break;
        }

        for (var s = 0; s < cellCount; s++)
        {
            var stripe = stripes[s].DivideByWidth(new[] { .125f, .75f });
            stripe[0].Width = stripes[s].Width;
            stripe[0].X = stripes[s].X;
            stripe[0].TiOff.x = 1;
            pgRefs.Bg[s] = BuildPrimitive(trans, "Bg " + (s + 1).ToString("00"), PrimitiveType.Quad, stripe[0],
                0.002f);
            pgRefs.Fg[s] = BuildPrimitive(trans, "Fg " + (s + 1).ToString("00"), PrimitiveType.Quad, stripe[1],
                0.001f);
            var hint = new Dimensions.Box
            {
                Height = .14f,
                Width = .1f,
                X = stripe[1].X + stripe[1].Width / 2 - .1f,
                Y = stripe[1].Y - stripe[1].Height / 2 + .12f,
            };
            if (cellCount == ShreddedCells)
            {
                hint.X -= 10;
            }

            pgRefs.Hint[s] = BuildTMP(trans, "Hint " + (s + 1).ToString("00"), hint, 0);
        }
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

    private static CellDisplay BuildPrimitive(Transform trans, string name, PrimitiveType pt, Dimensions.Box b, float z)
    {
        var go = GameObject.CreatePrimitive(pt);
        go.name = name;
        go.transform.parent = trans;
        go.transform.localPosition = new Vector3(b.X, b.Y, z);
        go.transform.localScale = new Vector3(b.Width, b.Height, .1f);

        var collider = go.GetComponent<Collider>();
        DestroyImmediate(collider);

        var rend = go.GetComponent<Renderer>();
        rend.sharedMaterial = Resources.Load<Material>("Materials/ThomasMountain");
        return new CellDisplay
        {
            Renderer = rend,
            TilingOffset = b.TiOff
        };
    }

    private static TextMeshPro BuildTMP(Transform trans, string name, Dimensions.Box b, float z)
    {
        var go = new GameObject
        {
            name = name,
            transform =
            {
                parent = trans,
                localPosition = new Vector3(b.X, b.Y, z),
                localScale = new Vector3(b.Width, b.Height, .1f)
            }
        };

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/CherryCreamSoda-Regular SDF");
        tmp.UpdateFontAsset();
        tmp.fontSize = 8;
        tmp.alignment = TextAlignmentOptions.Center;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x / rt.rect.width, rt.sizeDelta.y / rt.rect.height);

        return tmp;
    }

    private static void SetRenderProps(CellDisplay cd, Color color, Texture2D t, int textureSet)
    {
        var displayWidth = Screen.width;
        var displayHeight = Screen.height;
        var backgroundHeight = 1f / displayWidth * displayHeight;
        var photoWidthPercent = 218 / backgroundHeight / 178;
        var propBlock = new MaterialPropertyBlock();
        if (textureSet == -1)
        {
            propBlock.SetColor(Color1, color);
            propBlock.SetTexture(MainTex, Texture2D.whiteTexture);
            propBlock.SetVector(MainTexSt, new Vector4(1, 1, 0, 0));
        }
        else
        {
            var xOffset = cd.TilingOffset.z;
            if (xOffset == 0)
            {
                xOffset = (1 - photoWidthPercent) / 2;
            }
            else
            {
                xOffset = (xOffset - .5f) * photoWidthPercent + .5f;
            }

            propBlock.SetColor(Color1, Color.white);
            propBlock.SetTexture(MainTex, t);
            propBlock.SetVector(MainTexSt, new Vector4(
                cd.TilingOffset.x * photoWidthPercent,
                cd.TilingOffset.y,
                xOffset,
                cd.TilingOffset.w
            ));
        }

        cd.Renderer.SetPropertyBlock(propBlock);
    }

    private float[] GetValues(bool setInputOffset)
    {
        rawValues = RawInputs.Get(rawValues, mockGyroscope);

        var values = new float[cellCount];
        for (var i = 0; i < cellCount; i++)
        {
            if (cells[i].HeldFor >= HoldForToScore)
            {
                values[i] = cells[i].HeldOnValue;
                continue;
            }

            values[i] = GetVal(rawValues, cells[i], Notches);

            if (setInputOffset && cells[i].Forward)
            {
                cells[i].InputOffset = (cells[i].Start + Notches - values[i]) % Notches;
            }
            else if (setInputOffset)
            {
                cells[i].InputOffset = (Notches - cells[i].Start + Notches - values[i]) % Notches;
            }

            values[i] = (values[i] + cells[i].InputOffset) % Notches;
            if (Math.Abs(snapMode - 1f) < .001f)
            {
                values[i] = Mathf.Floor(values[i] + .5f);
            }
            else if (Math.Abs(snapMode - .5f) < .001f)
            {
                values[i] = Mathf.Floor(values[i] * 2 + .5f) / 2;
            }
        }

        return values;
    }

    private static float GetVal(IReadOnlyList<float> rawValues, Challenger.Cell cell, int notches)
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
            val = rawValues[cell.Input] * rainbowRepeats * notches;
        }
        else
        {
            val = (1 - rawValues[cell.Input]) * rainbowRepeats * notches;
        }

        return val % notches;
    }
}