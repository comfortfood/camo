using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class GameController : MonoBehaviour
{
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

    public GameObject[] playground = { };
    public GameObject mockGyroscope;

    private Renderer _renderer;
    private MaterialPropertyBlock propBlock;

    // config
    private int colorSet;
    private float startTime;
    private int stripes = 2;
    private const int Notches = 5;
    private const int InputTypes = 3;
    private const int HoldForToScore = 30;
    private const float CloseEnough = 0.2F;
    private const int RainbowRepeats = 4;
    private const int RainbowRepeatsPitch = 3;
    private int score;
    private int target1 = -1;
    private int target2 = -1;
    private int target3 = -1;
    private int chosenInput1 = -1;
    private int chosenInput2 = -1;
    private int chosenInput3 = -1;
    private int heldFor;
    private float[] rawInputs;
    private static readonly int Color1 = Shader.PropertyToID("_Color");

    public void Start()
    {
        startTime = Time.realtimeSinceStartup;
        Input.gyro.enabled = true;
    }

    public void Update()
    {
        stripes = 3;
        var timeNow = Time.realtimeSinceStartup;
        colorSet = (int)(timeNow - startTime) % colorSets.Length;
        rawInputs = RawInputs.Get(rawInputs, mockGyroscope);

        if (colorSets.Length <= 0 || colorSets[colorSet].colors.Length <= 5) return;
        foreach (var obj in playground)
        {
            var trans = obj.transform;

            var childTrans = trans.Find("Text 01");
            var tmp = childTrans.GetComponent<TextMeshPro>();
            tmp.color = colorSets[colorSet].colors[0];

            childTrans = trans.Find("Text 02");
            tmp = childTrans.GetComponent<TextMeshPro>();
            tmp.color = colorSets[colorSet].colors[0];

            childTrans = trans.Find("Fg 01");
            if (childTrans == null)
            {
                BuildMeshes(trans);
            }

            float tmpRr = chosenInput1 == 0 ? RainbowRepeatsPitch : RainbowRepeats;
            var val1 = GetVal(rawInputs, chosenInput1, tmpRr, Notches);

            tmpRr = chosenInput2 == 0 ? RainbowRepeatsPitch : RainbowRepeats;
            var val2 = GetVal(rawInputs, chosenInput2, tmpRr, Notches);

            tmpRr = chosenInput3 == 0 ? RainbowRepeatsPitch : RainbowRepeats;
            var val3 = GetVal(rawInputs, chosenInput3, tmpRr, Notches);

            if (chosenInput1 != -1 && chosenInput2 != -1 && chosenInput3 != -1)
            {
                if ((Mathf.Abs(val1 - target1) <= CloseEnough ||
                     (target1 == 0 && Mathf.Abs(val1 - Notches) <= CloseEnough)) &&
                    (Mathf.Abs(val2 - target2) <= CloseEnough ||
                     (target2 == 0 && Mathf.Abs(val2 - Notches) <= CloseEnough)) &&
                    (Mathf.Abs(val3 - target3) <= CloseEnough ||
                     (target3 == 0 && Mathf.Abs(val3 - Notches) <= CloseEnough)))
                {
                    heldFor++;
                    // Debug.Log("heldFor: " + heldFor);
                }
                else
                {
                    heldFor = 0;
                }
            }

            if ((chosenInput1 == -1 && chosenInput2 == -1 && chosenInput3 == -1) || heldFor >= HoldForToScore)
            {
                if (heldFor >= HoldForToScore)
                {
                    score++;
                    Handheld.Vibrate();
                }

                heldFor = 0;

                target1 = RandIntn(Notches);
                target2 = RandIntn(Notches);
                target3 = RandIntn(Notches);

                SetMeshColor(trans, "Bg Top", Colors.CellColor(target1, Notches, colorSets[colorSet].colors));
                SetMeshColor(trans, "Bg 01", Colors.CellColor(target1, Notches, colorSets[colorSet].colors));
                SetMeshColor(trans, "Bg 02", Colors.CellColor(target2, Notches, colorSets[colorSet].colors));
                SetMeshColor(trans, "Bg 03", Colors.CellColor(target3, Notches, colorSets[colorSet].colors));
                SetMeshColor(trans, "Bg Bottom", Colors.CellColor(target3, Notches, colorSets[colorSet].colors));

                chosenInput1 = RandIntn(InputTypes);
                chosenInput2 = RandIntn(InputTypes);
                chosenInput3 = RandIntn(InputTypes);

                chosenInput1 = 0;
                chosenInput2 = 1;
                chosenInput3 = 2;
            }

            SetMeshColor(trans, "Fg 01", Colors.CellColor(val1, Notches, colorSets[colorSet].colors));
            SetMeshColor(trans, "Fg 02", Colors.CellColor(val2, Notches, colorSets[colorSet].colors));
            SetMeshColor(trans, "Fg 03", Colors.CellColor(val3, Notches, colorSets[colorSet].colors));

            SetMeshColor(trans, "Score", colorSets[colorSet].colors[1]);

            SetMeshColor(trans, "Z", Color.yellow);
        }
    }

    private int RandIntn(int n)
    {
        var x = Mathf.FloorToInt(Random.Range(0, n));
        if (Mathf.Approximately(x, n))
        {
            x = 0;
        }

        return x;
    }

    private void BuildMeshes(Transform trans)
    {
        float displayWidth = Screen.width;
        float displayHeight = Screen.height;
        const float goldenRatio = 1.618F;

        const float overlap = 0.01F;

        const float foregroundWidth = 3.0F / 4;
        const float foregroundHeight = foregroundWidth * goldenRatio;

        var backgroundHeight = 1.0F / displayWidth * displayHeight;

        var topHeight = (backgroundHeight - foregroundHeight) / 2;

        var scoreHeight = topHeight / 5;

        BuildMesh(trans, "Score", PrimitiveType.Quad,
            new Vector3(0, backgroundHeight / 2 - scoreHeight / 2 - 1.0F / 5, 0.001F),
            new Vector3(7.0F / 8, scoreHeight, 1));
        BuildMesh(trans, "Bg Top", PrimitiveType.Quad,
            new Vector3(0, foregroundHeight / 2 + topHeight / 2 - overlap / 2, 0.003F),
            new Vector3(1, topHeight + overlap, 1));

        // if (stripes == 1)
        // {
        //     buildMesh(trans, "Fg 01", PrimitiveType.Quad, new Vector3(0, 0, 0.001F),
        //         new Vector3(foregroundWidth, foregroundHeight, 1));
        //     buildMesh(trans, "Bg 01", PrimitiveType.Quad, new Vector3(0, 0, 0.002F),
        //         new Vector3(1, foregroundHeight, 1));
        // }
        // else if (stripes == 2)
        // {
        //     float restHeight = 2.0F * backgroundHeight / 5 - topHeight;
        //     float oneHeight = foregroundHeight - restHeight;
        //     buildMesh(trans, "Fg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, oneHeight, 1));
        //     buildMesh(trans, "Fg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     buildMesh(trans, "Bg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, oneHeight, 1));
        //     buildMesh(trans, "Bg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, restHeight, 1));
        // }
        // else
        if (stripes == 3)
        {
            float oneHeight = backgroundHeight / 3 - topHeight;
            float restHeight = (foregroundHeight - oneHeight) / 2;
            BuildMesh(trans, "Fg 01", PrimitiveType.Quad,
                new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0.001F),
                new Vector3(foregroundWidth, oneHeight, 1));
            BuildMesh(trans, "Fg 02", PrimitiveType.Quad,
                new Vector3(0, restHeight * 1.5F - foregroundHeight / 2, 0.001F),
                new Vector3(foregroundWidth, restHeight, 1));
            BuildMesh(trans, "Fg 03", PrimitiveType.Quad,
                new Vector3(0, restHeight * 0.5F - foregroundHeight / 2, 0.001F),
                new Vector3(foregroundWidth, restHeight, 1));
            BuildMesh(trans, "Bg 01", PrimitiveType.Quad,
                new Vector3(0, oneHeight / 2 + restHeight * 2 - foregroundHeight / 2, 0.002F),
                new Vector3(1, oneHeight, 1));
            BuildMesh(trans, "Bg 02", PrimitiveType.Quad,
                new Vector3(0, restHeight * 1.5F - foregroundHeight / 2, 0.002F),
                new Vector3(1, restHeight, 1));
            BuildMesh(trans, "Bg 03", PrimitiveType.Quad,
                new Vector3(0, restHeight / 2 - foregroundHeight / 2, 0.002F),
                new Vector3(1, restHeight, 1));
        }
        // else if (stripes == 4)
        // {
        //     float oneHeight = backgroundHeight / 3 - topHeight;
        //     float restHeight = (foregroundHeight - oneHeight) / 4;
        //     buildMesh(trans, "Fg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight * 4 - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, oneHeight, 1));
        //     buildMesh(trans, "Fg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 3.5F - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     buildMesh(trans, "Fg 03", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 2.5F - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     buildMesh(trans, "Fg 04", PrimitiveType.Quad,
        //         new Vector3(0, restHeight - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, restHeight * 2.0F, 1));
        //     buildMesh(trans, "Bg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight * 4 - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, oneHeight, 1));
        //     buildMesh(trans, "Bg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 3.5F - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, restHeight, 1));
        //     buildMesh(trans, "Bg 03", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 2.5F - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, restHeight, 1));
        //     buildMesh(trans, "Bg 04", PrimitiveType.Quad,
        //         new Vector3(0, restHeight - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, restHeight * 2.0F, 1));
        // }
        // else if (stripes == 5)
        // {
        //     float oneHeight = backgroundHeight / 3 - topHeight;
        //     float restHeight = (foregroundHeight - oneHeight) / 5;
        //     buildMesh(trans, "Fg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight * 5 - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, oneHeight, 1));
        //     buildMesh(trans, "Fg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 4.5F - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     buildMesh(trans, "Fg 03", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 3.5F - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     buildMesh(trans, "Fg 04", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 2.5F - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, restHeight, 1));
        //     buildMesh(trans, "Fg 05", PrimitiveType.Quad,
        //         new Vector3(0, restHeight - foregroundHeight / 2, 0.001F),
        //         new Vector3(foregroundWidth, restHeight * 2, 1));
        //     buildMesh(trans, "Bg 01", PrimitiveType.Quad,
        //         new Vector3(0, oneHeight / 2 + restHeight * 5 - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, oneHeight, 1));
        //     buildMesh(trans, "Bg 02", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 4.5F - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, restHeight, 1));
        //     buildMesh(trans, "Bg 03", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 3.5F - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, restHeight, 1));
        //     buildMesh(trans, "Bg 04", PrimitiveType.Quad,
        //         new Vector3(0, restHeight * 2.5F - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, restHeight, 1));
        //     buildMesh(trans, "Bg 05", PrimitiveType.Quad,
        //         new Vector3(0, restHeight - foregroundHeight / 2, 0.002F),
        //         new Vector3(1, restHeight * 2, 1));
        // }

        BuildMesh(trans, "Bg Bottom", PrimitiveType.Quad,
            new Vector3(0, -foregroundHeight / 2 - topHeight / 2 + overlap / 2, 0.003F),
            new Vector3(1, topHeight + overlap, 1));

        BuildMesh(trans, "Z", PrimitiveType.Cube, new Vector3(0, 0, 0.06F),
            new Vector3(1.15F, backgroundHeight + 0.15F, 0.1F));
    }

    private static void BuildMesh(Transform trans, string name, PrimitiveType pt, Vector3 localPosition,
        Vector3 localScale)
    {
        GameObject go = GameObject.CreatePrimitive(pt);
        go.name = name;
        Renderer rend = go.GetComponent<Renderer>();
        Collider collider = go.GetComponent<Collider>();
        DestroyImmediate(collider);
        rend.sharedMaterial = Resources.Load("Materials/ThomasMountain") as Material;
        go.transform.parent = trans;
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
    }

    private void SetMeshColor(Transform trans, string childName, Color color)
    {
        Transform childTrans = trans.Find(childName);
        propBlock = new MaterialPropertyBlock();
        _renderer = childTrans.GetComponent<Renderer>();
        _renderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(Color1, color);
        _renderer.SetPropertyBlock(propBlock);
    }

    private static float GetVal(IReadOnlyList<float> inputs, int chosenInput, float rainbowRepeats, int notches)
    {
        if (chosenInput == -1) return -1;
        return (inputs[chosenInput] * rainbowRepeats) % 1.0F * notches;
    }
}