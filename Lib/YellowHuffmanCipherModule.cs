using System;
using System.Collections.Generic;
using System.Linq;
using CompressionCiphersLib;
using KModkit;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class YellowHuffmanCipherModule : HuffmanModuleBase
{
    private static int _moduleIdCounter = 1;
    private int _moduleId;

    protected override string loggingTag => $"Yellow Huffman Cipher Cipher #{_moduleId}";

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        var wordData = new Data();
        var keyword = wordData.ChooseWord(7, 8);
        _answer = wordData.ChooseWord(4, 8);

        var keywordReduced = keyword.Distinct().JoinString("");
        var restOfAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Where(ch => !keywordReduced.Contains(ch)).JoinString("");
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
        if (encodedData.Length > 20)
            goto tryAgain;

        Debug.LogFormat("[Yellow Huffman Cipher #{0}] Letters {1}", _moduleId, encodedData);
        Debug.LogFormat("[Yellow Huffman Cipher #{0}] Binary 0 {1}", _moduleId, encodedPieces.JoinString(";"));

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
    }
}