using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HuffmanCipher;
using UnityEngine;
using Words;

using Rnd = UnityEngine.Random;

public class BlueHuffmanCipherModule : MonoBehaviour
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

    private List<string> _wordList;
    private string[][] _pages = { new string[3], new string[3], new string[3] };
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
        var encBinaryLogMessages = new List<string>();
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
        treeLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] Letters on the module: {1}", _moduleId, fakeAlphabetScores));

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

            var newNode = new HuffmanBranch(lowestScoreNode, secondLowestScoreNode);
            nodes.RemoveAt(Math.Max(lowestIx, secondLowestIx));
            nodes.RemoveAt(Math.Min(lowestIx, secondLowestIx));
            nodes.Add(newNode);
            treeLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] Combining {1} ({2}) and {3} ({4}), tree is now: {5}",
                _moduleId, lowestScoreNode, lowestScoreNode.Score, secondLowestScoreNode, secondLowestScoreNode.Score, nodes.Join(", ")));
        }
        var root = nodes[0];
        treeLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] Finished tree: {1}", _moduleId, root));

        var encodedBits = new List<int>();
        var prevEncodedIx = 0;
        foreach (var letter in _answer)
        {
            encodedBits.AddRange(root.EncodeBits(letter));
            encWordLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] {1} decodes to {2}", _moduleId, encodedBits.Skip(prevEncodedIx).Join(""), letter));
            prevEncodedIx = encodedBits.Count;
        }

        var encodedWord = "";
        var encodedBitsStr = encodedBits.Join("");
        while (encodedBits.Count > 0)
        {
            if (encodedBits.Count < 4)
                goto tryAgain;

            if (encodedBits[0] != 0 && (encodedBits[1] != 0 || encodedBits[2] != 0))
            {
                encodedWord += (char) ('A' + 10 + ((encodedBits[0] << 3) | (encodedBits[1] << 2) | (encodedBits[2] << 1) | (encodedBits[3])));
                encBinaryLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] {1} becomes {2}", _moduleId, encodedWord.Last(), encodedBits.Take(4).Join("")));
                encodedBits.RemoveRange(0, 4);
            }
            else
            {
                if (encodedBits.Count < 5)
                    goto tryAgain;

                encodedWord += (char) ('A' + ((encodedBits[0] << 4) | (encodedBits[1] << 3) | (encodedBits[2] << 2) | (encodedBits[3] << 1) | (encodedBits[4])));
                encBinaryLogMessages.Add(string.Format("[Blue Huffman Cipher #{0}] {1} becomes {2}", _moduleId, encodedWord.Last(), encodedBits.Take(5).Join("")));
                encodedBits.RemoveRange(0, 5);
            }
        }

        if (encodedWord.Length > 7)
            goto tryAgain;

        foreach (var msg in treeLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] Encoded word: {1}", _moduleId, encodedWord);
        foreach (var msg in encBinaryLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] String of binary: {1}", _moduleId, encodedBitsStr);
        foreach (var msg in encWordLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] Solution word: {1}", _moduleId, _answer);

        _pages[1][2] = encodedWord;

        getScreens();
    }

    void MakeExample()
    {
        var answer = "FACADE";

        tryAgain:

        var treeLogMessages = new List<string>();
        var encBinaryLogMessages = new List<string>();
        var encWordLogMessages = new List<string>();

        var fakeAlphabetScores = "";
        for (var i = 0; i < 6; i++)
            fakeAlphabetScores += (char) ('A' + Rnd.Range(0, 26));

        var nodes = fakeAlphabetScores.Select((ch, ix) => (HuffmanNode) new HuffmanLeaf(ch - 'A' + 1, (char) ('A' + ix))).ToList();
        treeLogMessages.Add(string.Format("EXAMPLE — Letters on the module: {1}", _moduleId, fakeAlphabetScores));

        var iteration = 0;
        int? hadTie1 = null;
        int? hadTie2 = null;

        while (nodes.Count > 1)
        {
            iteration++;
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

            if (hadTie1 == null && lowestScoreNode.Score != secondLowestScoreNode.Score &&
                nodes.Any(n => n != secondLowestScoreNode && n.Score == secondLowestScoreNode.Score))
                hadTie1 = iteration;

            if (hadTie2 == null && lowestScoreNode.Score == secondLowestScoreNode.Score)
                hadTie2 = iteration;

            var newNode = new HuffmanBranch(lowestScoreNode, secondLowestScoreNode);
            nodes.RemoveAt(Math.Max(lowestIx, secondLowestIx));
            nodes.RemoveAt(Math.Min(lowestIx, secondLowestIx));
            nodes.Add(newNode);
            treeLogMessages.Add(string.Format("EXAMPLE — Combining {1} ({2}) and {3} ({4}), tree is now: {5}",
                _moduleId, lowestScoreNode, lowestScoreNode.Score, secondLowestScoreNode, secondLowestScoreNode.Score, nodes.Join(", ")));
        }
        if (hadTie1 == null || hadTie2 == null || hadTie2.Value <= hadTie1.Value)
            goto tryAgain;
        var root = nodes[0];
        treeLogMessages.Add(string.Format("EXAMPLE — Finished tree: {1}", _moduleId, root));

        var encodedBits = new List<int>();
        var prevEncodedIx = 0;
        foreach (var letter in answer)
        {
            encodedBits.AddRange(root.EncodeBits(letter));
            encWordLogMessages.Add(string.Format("EXAMPLE — {1} decodes to {2}", _moduleId, encodedBits.Skip(prevEncodedIx).Join(""), letter));
            prevEncodedIx = encodedBits.Count;
        }

        var encodedWord = "";
        var encodedBitsStr = encodedBits.Join("");
        while (encodedBits.Count > 0)
        {
            if (encodedBits.Count < 4)
                goto tryAgain;

            if (encodedBits[0] != 0 && (encodedBits[1] != 0 || encodedBits[2] != 0))
            {
                encodedWord += (char) ('A' + 10 + ((encodedBits[0] << 3) | (encodedBits[1] << 2) | (encodedBits[2] << 1) | (encodedBits[3])));
                encBinaryLogMessages.Add(string.Format("EXAMPLE — {1} becomes {2}", _moduleId, encodedWord.Last(), encodedBits.Take(4).Join("")));
                encodedBits.RemoveRange(0, 4);
            }
            else
            {
                if (encodedBits.Count < 5)
                    goto tryAgain;

                encodedWord += (char) ('A' + ((encodedBits[0] << 4) | (encodedBits[1] << 3) | (encodedBits[2] << 2) | (encodedBits[3] << 1) | (encodedBits[4])));
                encBinaryLogMessages.Add(string.Format("EXAMPLE — {1} becomes {2}", _moduleId, encodedWord.Last(), encodedBits.Take(5).Join("")));
                encodedBits.RemoveRange(0, 5);
            }
        }

        if (encodedWord.Length > 7)
            goto tryAgain;

        foreach (var msg in treeLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("EXAMPLE — Encoded word: {1}", _moduleId, encodedWord);
        foreach (var msg in encBinaryLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("EXAMPLE — String of binary: {1}", _moduleId, encodedBitsStr);
        foreach (var msg in encWordLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("EXAMPLE — Solution word: {1}", _moduleId, answer);
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
                Module.HandlePass();
                _moduleSolved = true;
                ScreenTexts[2].text = "";
            }
            else
            {
                Audio.PlaySoundAtTransform(Sounds[3].name, transform);
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
    private string TwitchHelpMessage = "Move to other screens using !{0} right|left|r|l|. Submit the decrypted word with !{0} submit qwertyuiopasdfghjklzxcvbnm";
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
