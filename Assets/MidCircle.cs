using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class MidCircle : MonoBehaviour
{
    public KMBombModule module;
    public KMBombInfo bombInfo;
    public KMAudio Audio;
    public KMSelectable[] buttons;
    public Renderer[] wedges;
    public TextMesh[] texts;
    public Material[] materials;
    public GameObject circle;
    public KMRuleSeedable ruleSeed;
    public KMColorblindMode colorblindMode;
    public Color gray;

    private int moduleId;
    private static int moduleIdCounter = 1;
    private bool moduleSolved;
    private static readonly string[] colorNames = new string[8] { "red", "orange", "yellow", "green", "blue", "magenta", "white", "black" };
    private static readonly Color[] colors = new Color[] {
        new Color(0.800f, 0.000f, 0.000f, 1.000f),
        new Color(0.800f, 0.452f, 0.033f, 1.000f),
        new Color(0.791f, 0.800f, 0.133f, 1.000f),
        new Color(0.000f, 1.000f, 0.131f, 1.000f),
        new Color(0.101f, 0.070f, 0.956f, 1.000f),
        new Color(0.800f, 0.082f, 0.780f, 1.000f),
        new Color(1.000f, 1.000f, 1.000f, 1.000f),
        new Color(0.074f, 0.074f, 0.074f, 1.000f)
    };

    private bool colorblindbool;
    private int[] shuffle = new int[8];
    private bool isClockwise;
    private Coroutine spin;
    private int baseColor;
    private int[] spaces = new int[8];
    private int pressCount = 0;
    private int[][] infoTable = new int[7][] { new int[8], new int[8], new int[8], new int[8], new int[8], new int[8], new int[8] };
    private bool trueModuleSolved;
    private bool[] fading = new bool[8];
    private Coroutine[] fadingAnimations = new Coroutine[8];

    private void Start()
    {
        moduleId = moduleIdCounter++;
        colorblindbool = colorblindMode.ColorblindModeActive;
        SetColorBlind(colorblindbool);
        var rs = ruleSeed.GetRNG();
        for (int i = 0; i < 7; i++)
        {
            infoTable[i] = Enumerable.Range(0, 8).ToArray();
            rs.ShuffleFisherYates(infoTable[i]);
            for (int j = 0; j < 20; j++)
                rs.Next(0, 2);
        }
        shuffle = Enumerable.Range(0, 8).ToArray().Shuffle();
        int num = rnd.Range(0, 2);
        if (num == 0)
            isClockwise = false;
        else
            isClockwise = true;
        for (int i = 0; i < wedges.Length; i++)
        {
            wedges[i].material = materials[shuffle[i]];
            texts[i].text = colorNames[shuffle[i]];
            texts[i].color = colors[shuffle[i]];
        }
        spin = StartCoroutine(Spin());
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].OnInteract += ButtonPress(i);
        var numbers = bombInfo.GetSerialNumberNumbers().ToArray();
        int color = 0;
        for (int i = 0; i < numbers.Length; i++)
            color += numbers[i];
        color %= 8;
        baseColor = color;
        Debug.LogFormat("[Mid Circle #{0}] The base color is {1}.", moduleId, colorNames[baseColor]);
        int index = Array.IndexOf(shuffle, color);
        for (int i = 0; i < spaces.Length; i++)
        {
            int oldColor = color;
            int progress = 0;
            while (shuffle[index] != (oldColor + 1) % 8)
            {
                progress++;
                index = (index + (isClockwise ? 1 : 7)) % 8;
            }
            spaces[i] = progress - 1;
            color = (oldColor + 1) % 8;
        }
        Debug.LogFormat("[Mid Circle #{0}] The number of spaces between each color are: {1}", moduleId, spaces.Select(i => i+1).Join(", "));
        Debug.LogFormat("[Mid Circle #{0}] The colors to press in order are: {1} {2} {3} {4} {5} {6} {7} {8}", moduleId, colorNames[infoTable[spaces[0]][0]], colorNames[infoTable[spaces[1]][1]], colorNames[infoTable[spaces[2]][2]], colorNames[infoTable[spaces[3]][3]], colorNames[infoTable[spaces[4]][4]], colorNames[infoTable[spaces[5]][5]], colorNames[infoTable[spaces[6]][6]], colorNames[infoTable[spaces[7]][7]]);
    }

    private void SetColorBlind(bool colorblindbool)
    {
        for (int i = 0; i < texts.Length; i++)
            texts[i].gameObject.SetActive(colorblindbool);
    }

    private KMSelectable.OnInteractHandler ButtonPress(int i)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, buttons[i].transform);
            buttons[i].AddInteractionPunch();
            if (moduleSolved)
                return false;
            if (shuffle[i] == infoTable[spaces[pressCount]][pressCount])
            {
                Debug.LogFormat("[Mid Circle #{0}] Pressed {1} correctly.", moduleId, colorNames[shuffle[i]]);
                pressCount++;
                if (pressCount == 8)
                {
                    Debug.LogFormat("[Mid Circle #{0}] The module has been solved! You are mid.", moduleId);
                    moduleSolved = true;
                    for (int j = 0; j < 8; j++)
                        fadingAnimations[j] = StartCoroutine(FadeColor(j, gray, 2.5f));
                }
            }
            else
            {
                Debug.LogFormat("[Mid Circle #{0}] Pressed {1}, when {2} was expected. Strike.", moduleId, colorNames[shuffle[i]], colorNames[infoTable[spaces[pressCount]][pressCount]]);
                pressCount = 0;
                module.HandleStrike();
            }
            return false;
        };
    }

    private IEnumerator Spin()
    {
        while (true)
        {
            float duration = 35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                circle.transform.localEulerAngles = new Vector3(0f, Mathf.Lerp(0f, isClockwise ? 360f : -360f, elapsed / duration), 0f);
                yield return null;
                elapsed += Time.deltaTime;
            }
        }
    }

    private IEnumerator FadeColor(int i, Color end, float duration)
    {
        var start = wedges[i].material.color;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            wedges[i].material.color = Color.Lerp(start, end, elapsed / duration);
            texts[i].color = Color.Lerp(start, end, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        wedges[i].material.color = end;
        module.HandlePass();
        if (spin != null)
            StopCoroutine(spin);
        circle.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
        trueModuleSolved = true;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} press r o y g b m w k | [Presses red, orange, yellow, green, blue, magenta, white, black.] | Press the wedges by the first letter of their color. | 'press' is optional.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (command.StartsWith("press "))
            command = command.Substring(6);
        else if (command.StartsWith("submit "))
            command = command.Substring(7);
        var list = new List<KMSelectable>();
        var cols = "roygbmwk ".ToCharArray();
        for (int i = 0; i < command.Length; i++)
        {
            int ix = Array.IndexOf(cols, command[i]);
            if (ix == 8)
                continue;
            if (ix == -1)
                yield break;
            list.Add(buttons[Array.IndexOf(shuffle, ix)]);
        }
        yield return null;
        yield return "solve";
        foreach (var b in list)
        {
            b.OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        while (pressCount < 8)
        {
            buttons[Array.IndexOf(shuffle, infoTable[spaces[pressCount]][pressCount])].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
        while (!trueModuleSolved)
            yield return true;
    }
}