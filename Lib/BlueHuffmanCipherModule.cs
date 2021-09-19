using System;
using System.Collections.Generic;
using System.Linq;
using CipherModulesLib;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class BlueHuffmanCipherModule : HuffmanModuleBase
{
    private static int _moduleIdCounter = 1;
    private int _moduleId;

    protected override string loggingTag => $"Blue Huffman Cipher Cipher #{_moduleId}";

    void Start()
    {
        _moduleId = _moduleIdCounter++;
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
                encodedPieces.Add(string.Format("{0}={1}", encodedData.Last(), encodedBits.Take(4).JoinString()));
                encodedBits.RemoveRange(0, 4);
            }
            else
            {
                if (encodedBits.Count < 5)
                    goto tryAgain;

                encodedData += (char) ('A' + ((encodedBits[0] << 4) | (encodedBits[1] << 3) | (encodedBits[2] << 2) | (encodedBits[3] << 1) | (encodedBits[4])));
                encodedPieces.Add(string.Format("{0}={1}", encodedData.Last(), encodedBits.Take(5).JoinString()));
                encodedBits.RemoveRange(0, 5);
            }
        }

        if (encodedData.Length > 7)
            goto tryAgain;

        Debug.LogFormat("[Blue Huffman Cipher #{0}] Letters {1}", _moduleId, fakeAlphabetScores);
        foreach (var msg in treeLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] Binary 1 {1}", _moduleId, encodedPieces.JoinString(";"));
        foreach (var msg in encWordLogMessages)
            Debug.Log(msg);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] Solution {1}", _moduleId, _answer);
        Debug.LogFormat("[Blue Huffman Cipher #{0}] Solution word: {1}", _moduleId, _answer);

        _pages[1][2] = encodedData;
    }

    protected override int getFontSize(int page, int screen) =>
        page == 1 && screen == 2 && _pages[1][2].Count(ch => ch == 'W' || ch == 'M') > 4 ? 30 : 35;
}
