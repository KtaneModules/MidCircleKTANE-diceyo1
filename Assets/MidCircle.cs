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
    public GameObject[] wedges;
    public TextMesh[] texts;
    public Material[] materials;
    public GameObject circle;
    public KMRuleSeedable ruleSeed;
    public KMColorblindMode colorblindMode;

    private int moduleId;
    private static int moduleIdCounter = 1;
    private bool moduleSolved;
    private static readonly string[] colorNames = new string[8] { "red", "orange", "yellow", "green", "blue", "magenta", "white", "black" };
    private static readonly Color32[] colors = new Color32[] {
    new Color32(245,10,2,255),
    new Color32(240,90,0,255),
    new Color32(244,244,0,255),
    new Color32(2,242,2,255),
    new Color32(5,10,245,255),
    new Color32(240,0,240,255),
    new Color32(255,255,255,255),
    new Color32(0,0,0,255) };
    private bool colorblindbool;
    private int[] shuffle = new int[8];
    private bool isClockwise;
    private Coroutine spin;
    private int baseColor;
    private int[] spaces = new int[8];
    private int pressCount = 0;
    private int[][] infoTable = new int[7][]
    {
        new int[8],
        new int[8],
        new int[8],
        new int[8],
        new int[8],
        new int[8],
        new int[8]
    };

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
            wedges[i].GetComponent<MeshRenderer>().material = materials[shuffle[i]];
            texts[i].text = colorNames[shuffle[i]];
            texts[i].color = colors[shuffle[i]];
        }
        spin = StartCoroutine(Spin());
        for (int i = 0;i < buttons.Length;i++)
            buttons[i].OnInteract += ButtonPress(i);
        var numbers = bombInfo.GetSerialNumberNumbers().ToArray();
        int color = 0;
        for (int i = 0; i < numbers.Length;i++)
            color += numbers[i];
        color %= 8;
        baseColor = color;
        Debug.LogFormat("[Mid Circle #{0}] the base color is {1}", moduleId, colorNames[baseColor]);
        int index = Array.IndexOf(shuffle, color);
        for (int i = 0; i < spaces.Length;i++)
        {
            int oldColor = color;
            int progress = 0;
            while (shuffle[index] != (oldColor+1)%8)
            {
                progress++; 
                index = (index + (isClockwise ? 1 : 7)) % 8;
            }
            spaces[i] = progress-1;
            color = (oldColor+1)%8;
        }
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
                    module.HandlePass();
                }
            }
            else
            {
                Debug.LogFormat("[Mid Circle #{0}] Pressed {1}, when {2} was expected. Strike.", moduleId, shuffle[i], infoTable[spaces[pressCount]][pressCount]);
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
}