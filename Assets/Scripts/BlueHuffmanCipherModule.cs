using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HuffmanCipher;
using UnityEngine;
using Words;

using Rnd = UnityEngine.Random;

public class BlueHuffmanCipherModule : HuffmanBase
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

    private readonly string[][] _pages = { new string[3], new string[3] };
    private string _answer;
    private int _page;
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
        foreach (var keybutton in Keyboard)
            keybutton.OnInteract += letterPress(keybutton);
    }

    void Start()
    {
        // Selects all words of length 5 or more (allWords[0] is 4-letter words)
        _answer = new Data().ChooseWord(5, 8);

        tryAgain:

        var treeLogMessages = new List<string>();
        var encWordLogMessages = new List<string>();

        var fakeAlphabetScores = "";
        for (var i = 0; i < 26; i++)
            fakeAlphabetScores += (char) ('A' + Rnd.Range(0, 26));

        for (var i = 0; i < 3; i++)
            _pages[0][i] = fakeAlphabetScores.Substring(6 * i, 6);
        _pages[1][0] = fakeAlphabetScores.Substring(18, 6);
        _pages[1][1] = fakeAlphabetScores.Substring(24, 2);
        _page = 0;

        var nodes = fakeAlphabetScores.Select((ch, ix) => (HuffmanNode) new HuffmanLeaf(ch - 'A' + 1, (char) ('A' + ix))).ToList();

        while (nodes.Count > 1)
        {
            HuffmanNode lowestScoreNode = null;
            var lowestIx = 0;
            HuffmanNode secondLowestScoreNode = null;
            var secondLowestIx = 0;
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

            treeLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] Combining {1} {2} {3}",
                _moduleId, nodes.IndexOf(lowestScoreNode) + 1, nodes.IndexOf(secondLowestScoreNode) + 1, generateStepSvg(nodes, lowestScoreNode, secondLowestScoreNode, showScore: true)));

            var newNode = new HuffmanParentNode(lowestScoreNode, secondLowestScoreNode);
            nodes.RemoveAt(Math.Max(lowestIx, secondLowestIx));
            nodes.RemoveAt(Math.Min(lowestIx, secondLowestIx));
            nodes.Add(newNode);
        }
        var tree = nodes[0];
        treeLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] Final 0 {1}", _moduleId, generateStepSvgNoScore(tree)));

        var encodedBits = new List<int>();
        var prevEncodedIx = 0;
        foreach (var letter in _answer)
        {
            encodedBits.AddRange(tree.EncodeBits(letter));
            encWordLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] Decode {1} {2} {3}", _moduleId, prevEncodedIx, encodedBits.Count, letter));
            prevEncodedIx = encodedBits.Count;
        }

        var encodedData = "";
        var encodedPieces = new List<string>();
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

        if (encodedData.Length > 7)
            goto tryAgain;

        Debug.LogFormat("[Blue Huffman Cipher #{0}] Letters {1}", _moduleId, fakeAlphabetScores);
        foreach (var msg in treeLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] Binary 1 {1}", _moduleId, encodedPieces.Join(";"));
        foreach (var msg in encWordLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] Solution {1}", _moduleId, _answer);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] Solution word: {1}", _moduleId, _answer);

        _pages[1][2] = encodedData;

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
        if (_page == 0)
        {
            ScreenTexts[0].fontSize = 35;
            ScreenTexts[1].fontSize = 35;
            ScreenTexts[2].fontSize = 35;
        }
        else
        {
            ScreenTexts[0].fontSize = 35;
            ScreenTexts[1].fontSize = 35;
            ScreenTexts[2].fontSize = _pages[1][2].Count(ch => ch == 'W' || ch == 'M') > 4 ? 30 : 35;
        }
    }

    void submitWord(KMSelectable submitButton)
    {
        if (!_moduleSolved)
        {
            submitButton.AddInteractionPunch();
            if (ScreenTexts[2].text.Equals(_answer))
            {
                Audio.PlaySoundAtTransform(Sounds[2].name, transform);
                Debug.LogFormat("[Blue Huffman Cipher #{0}] Module solved.", _moduleId);
                Module.HandlePass();
                _moduleSolved = true;
                ScreenTexts[2].text = "";
            }
            else
            {
                Audio.PlaySoundAtTransform(Sounds[3].name, transform);
                Debug.LogFormat("[Blue Huffman Cipher #{0}] You submitted {1}. Strike!", _moduleId, ScreenTexts[2].text);
                Module.HandleStrike();
                _page = 0;
                getScreens();
                _submitScreen = false;
            }
        }
    }

    private KMSelectable.OnInteractHandler letterPress(KMSelectable pressed)
    {
        return delegate
        {
            if (!_moduleSolved)
            {
                pressed.AddInteractionPunch();
                Audio.PlaySoundAtTransform(Sounds[1].name, transform);
                if (_submitScreen)
                {
                    if (ScreenTexts[2].text.Length < 8)
                        ScreenTexts[2].text += pressed.GetComponentInChildren<TextMesh>().text;
                }
                else
                {
                    SubmitText.text = "SUB";
                    ScreenTexts[0].text = "";
                    ScreenTexts[1].text = "";
                    ScreenTexts[2].text = pressed.GetComponentInChildren<TextMesh>().text;
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
