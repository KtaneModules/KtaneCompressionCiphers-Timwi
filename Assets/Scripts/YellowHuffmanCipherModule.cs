using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HuffmanCipher;
using KModkit;
using UnityEngine;
using Words;

using Rnd = UnityEngine.Random;

public class YellowHuffmanCipherModule : HuffmanBase
{
    public TextMesh[] ScreenTexts;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public AudioClip[] Sounds;
    public KMAudio Audio;
    public TextMesh SubmitText;

    public KMSelectable LeftArrow;
    public KMSelectable RightArrow;
    public KMSelectable Submit;
    public KMSelectable[] Keyboard;

    private int _page = 0;
    private readonly string[][] _pages = { new string[3], new string[3] };
    private string _answer;
    private bool _submitScreen;
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    void Awake()
    {
        _moduleId = _moduleIdCounter++;

        LeftArrow.OnInteract += delegate { left(LeftArrow); return false; };
        RightArrow.OnInteract += delegate { right(RightArrow); return false; };
        Submit.OnInteract += delegate { submitWord(Submit); return false; };
        for (var keyIx = 0; keyIx < Keyboard.Length; keyIx++)
            Keyboard[keyIx].OnInteract += letterPress("QWERTYUIOPASDFGHJKLZXCVBNM"[keyIx], Keyboard[keyIx]);
    }

    void Start()
    {
        var wordData = new Data();
        var keyword = wordData.ChooseWord(7, 8);
        _answer = wordData.ChooseWord(4, 8);

        var keywordReduced = keyword.Distinct().Join("");
        var restOfAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Where(ch => !keywordReduced.Contains(ch)).Join("");
        var jumbledAlphabet = (Bomb.GetPortCount() & 2) != 0 ? restOfAlphabet + keywordReduced : keywordReduced + restOfAlphabet;

        Debug.LogFormat("[Yellow Huffman Cipher #{0}] Keyword: {1}", _moduleId, keyword);
        Debug.LogFormat("[Yellow Huffman Cipher #{0}] Jumbled alphabet: {1}", _moduleId, jumbledAlphabet);

        tryAgain:
        var treeLogMessages = new List<string>();
        var encBinaryLogMessages = new List<string>();
        var encWordLogMessages = new List<string>();

        // Generate a random Huffman tree
        var fakeAlphabetScores = "";
        for (var i = 0; i < 26; i++)
            fakeAlphabetScores += (char) ('A' + Rnd.Range(0, 26));
        var nodes = fakeAlphabetScores.Select((ch, ix) => (HuffmanNode) new HuffmanLeaf(ch - 'A' + 1, (char) ('A' + ix))).ToList();
        while (nodes.Count > 1)
        {
            HuffmanNode lowestScoreNode = null, secondLowestScoreNode = null;
            int lowestIx = 0, secondLowestIx = 0;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (lowestScoreNode == null || nodes[i].Score < lowestScoreNode.Score)
                {
                    secondLowestScoreNode = lowestScoreNode;
                    secondLowestIx = lowestIx;
                    lowestScoreNode = nodes[i];
                    lowestIx = i;
                }
                else if (secondLowestScoreNode == null || nodes[i].Score < secondLowestScoreNode.Score)
                {
                    secondLowestScoreNode = nodes[i];
                    secondLowestIx = i;
                }
            }

            var newNode = new HuffmanParentNode(lowestScoreNode, secondLowestScoreNode);
            nodes.RemoveAt(Math.Max(lowestIx, secondLowestIx));
            nodes.RemoveAt(Math.Min(lowestIx, secondLowestIx));
            nodes.Add(newNode);
        }
        var tree = nodes[0];

        // Populate the tree with the jumbled alphabet
        var alphabetIx = 0;
        tree.Populate(jumbledAlphabet, ref alphabetIx);
        treeLogMessages.Add(string.Format("[Yellow Huffman Cipher #{0}] Final 1 {1}", _moduleId, generateStepSvgNoScore(tree, useDepthInfo: true)));

        // Encode everything in letters
        var encodedBits = new List<int>();
        encodedBits.AddRange(tree.EncodeTreeStructure());
        var startEncodedIx = encodedBits.Count;
        var prevEncodedIx = encodedBits.Count;
        foreach (var letter in _answer)
        {
            encodedBits.AddRange(tree.EncodeBits(letter));
            encWordLogMessages.Add(string.Format("[Yellow Huffman Cipher #{0}] Decode {1} {2} {3}", _moduleId, prevEncodedIx, encodedBits.Count, letter));
            prevEncodedIx = encodedBits.Count;
        }

        var encodedPieces = new List<string>();
        var encodedData = "";
        while (encodedBits.Count > 0)
        {
            if (encodedBits.Count < 4)
                goto tryAgain;

            if (encodedBits[0] != 0 && (encodedBits[1] != 0 || encodedBits[2] != 0))
            {
                encodedData += (char) ('A' + 10 + ((encodedBits[0] << 3) | (encodedBits[1] << 2) | (encodedBits[2] << 1) | (encodedBits[3])));
                encodedPieces.Add(string.Format("{0}={1}", encodedData.Last(), encodedBits.Take(4).Join("")));
                encodedBits.RemoveRange(0, 4);
            }
            else
            {
                if (encodedBits.Count < 5)
                    goto tryAgain;

                encodedData += (char) ('A' + ((encodedBits[0] << 4) | (encodedBits[1] << 3) | (encodedBits[2] << 2) | (encodedBits[3] << 1) | (encodedBits[4])));
                encodedPieces.Add(string.Format("{0}={1}", encodedData.Last(), encodedBits.Take(5).Join("")));
                encodedBits.RemoveRange(0, 5);
            }
        }
        if (encodedData.Length > 20)
            goto tryAgain;

        Debug.LogFormat("[Yellow Huffman Cipher #{0}] Letters {1}", _moduleId, encodedData);
        Debug.LogFormat("[Yellow Huffman Cipher #{0}] Binary 0 {1}", _moduleId, encodedPieces.Join(";"));

        foreach (var msg in treeLogMessages)
            Debug.Log(msg);
        foreach (var msg in encBinaryLogMessages)
            Debug.Log(msg);
        foreach (var msg in encWordLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("[Yellow Huffman Cipher #{0}] Solution {1}", _moduleId, _answer);
        Debug.LogFormat("[Yellow Huffman Cipher #{0}] Solution word: {1}", _moduleId, _answer);

        var numLettersPerScreen = encodedData.Length / 4d;
        for (var screen = 0; screen < 4; screen++)
        {
            var start = (int) Math.Round(screen * numLettersPerScreen);
            var end = (int) Math.Round((screen + 1) * numLettersPerScreen);
            _pages[screen / 3][screen % 3] = encodedData.Substring(start, end - start);
        }

        _pages[1][2] = keyword;

        getScreens();
    }

    void left(KMSelectable arrow)
    {
        if (!_moduleSolved)
        {
            Audio.PlaySoundAtTransform(Sounds[0].name, transform);
            _submitScreen = false;
            arrow.AddInteractionPunch();
            _page = (_page + _pages.Length - 1) % _pages.Length;
            getScreens();
        }
    }

    void right(KMSelectable arrow)
    {
        if (!_moduleSolved)
        {
            Audio.PlaySoundAtTransform(Sounds[0].name, transform);
            _submitScreen = false;
            arrow.AddInteractionPunch();
            _page = (_page + 1) % _pages.Length;
            getScreens();
        }
    }

    private void getScreens()
    {
        SubmitText.text = (_page + 1) + "";
        ScreenTexts[0].text = _pages[_page][0] ?? "";
        ScreenTexts[1].text = _pages[_page][1] ?? "";
        ScreenTexts[2].text = _pages[_page][2] ?? "";
        ScreenTexts[2].fontSize = _page == 1 && ScreenTexts[2].text.Length > 6 ? 35 : 40;
    }

    void submitWord(KMSelectable submitButton)
    {
        if (!_moduleSolved)
        {
            submitButton.AddInteractionPunch();
            if (ScreenTexts[2].text.Equals(_answer))
            {
                Audio.PlaySoundAtTransform(Sounds[2].name, transform);
                Debug.LogFormat("[Yellow Huffman Cipher #{0}] Module solved.", _moduleId);
                Module.HandlePass();
                _moduleSolved = true;
                ScreenTexts[2].text = "";
            }
            else
            {
                Audio.PlaySoundAtTransform(Sounds[3].name, transform);
                Debug.LogFormat("[Yellow Huffman Cipher #{0}] You submitted {1}. Strike!", _moduleId, ScreenTexts[2].text);
                Module.HandleStrike();
                _page = 0;
                getScreens();
                _submitScreen = false;
            }
        }
    }

    private KMSelectable.OnInteractHandler letterPress(char letter, KMSelectable key)
    {
        return delegate
        {
            if (!_moduleSolved)
            {
                key.AddInteractionPunch();
                Audio.PlaySoundAtTransform(Sounds[1].name, transform);
                if (_submitScreen)
                {
                    if (ScreenTexts[2].text.Length < 8)
                        ScreenTexts[2].text += letter;
                }
                else
                {
                    SubmitText.text = "SUB";
                    ScreenTexts[0].text = "";
                    ScreenTexts[1].text = "";
                    ScreenTexts[2].text = letter.ToString();
                    _submitScreen = true;
                }
                var bigLetters = ScreenTexts[2].text.Count(ch => ch == 'W' || ch == 'M');
                ScreenTexts[2].fontSize = bigLetters > 5 ? 27 : bigLetters > 2 ? 30 : 35;
            }
            return false;
        };
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "!{0} right/left [move between screens] | !{0} submit answer";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.EqualsIgnoreCase("right") || command.EqualsIgnoreCase("r"))
        {
            yield return null;
            yield return new[] { RightArrow };
            yield break;
        }

        if (command.EqualsIgnoreCase("left") || command.EqualsIgnoreCase("l"))
        {
            yield return null;
            yield return new[] { LeftArrow };
            yield break;
        }

        var split = command.ToUpperInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2 || !split[0].Equals("SUBMIT") || split[1].Length > 8)
            yield break;
        var buttons = split[1].Select(getPositionFromChar).ToArray();
        if (buttons.Any(x => x < 0))
            yield break;

        yield return null;

        foreach (var let in split[1])
        {
            yield return new WaitForSeconds(.1f);
            Keyboard[getPositionFromChar(let)].OnInteract();
        }
        yield return new WaitForSeconds(.25f);
        Submit.OnInteract();
        yield return new WaitForSeconds(.1f);
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (_submitScreen && !_answer.StartsWith(ScreenTexts[2].text))
        {
            LeftArrow.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        for (var i = _submitScreen ? ScreenTexts[2].text.Length : 0; i < _answer.Length; i++)
        {
            Keyboard[getPositionFromChar(_answer[i])].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        Submit.OnInteract();
        yield return new WaitForSeconds(0.1f);
    }

    private int getPositionFromChar(char c)
    {
        return "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(c);
    }
}
