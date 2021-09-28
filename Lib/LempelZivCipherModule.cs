using System;
using System.Linq;
using CipherModulesLib;
using UnityEngine;

public partial class LempelZivCipherModule : CipherModuleBase
{
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    protected override string loggingTag => $"Lempel-Ziv Cipher #{_moduleId}";

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        var puzzle = LempelZivPuzzle.MakePuzzle();

        Debug.Log($"[Lempel-Ziv Cipher #{_moduleId}] Letters on module: {puzzle.EncodedData}");
        Debug.Log($"[Lempel-Ziv Cipher #{_moduleId}] Decoded binary: {puzzle.EncodedPieces.JoinString("; ")}");
        Debug.Log($"[Lempel-Ziv Cipher #{_moduleId}] Generated dictionary: {puzzle.Dictionary.Select(tup => Enumerable.Range(0, tup.length).Select(bit => (tup.value >> (tup.length - 1 - bit)) & 1).JoinString()).JoinString("; ")}");
        Debug.Log($"[Lempel-Ziv Cipher #{_moduleId}] Decoded bitmap ({puzzle.Width}×{puzzle.Height}): {puzzle.Bitmap.Take(puzzle.Bitmap.Length - 1).Select(b => b ? 1 : 0).JoinString()}\n{puzzle.Bitmap.Take(puzzle.Bitmap.Length - 1).Split(puzzle.Width).Select(row => row.Select(b => b ? "█" : "░").JoinString()).JoinString("\n")}"); ;
        Debug.Log($"[Lempel-Ziv Cipher #{_moduleId}] Solution: {puzzle.Answer} ({puzzle.AlphabetName})");
        _answer = puzzle.Answer;

        var numPages = 4;
        var numLettersPerScreen = puzzle.EncodedData.Length / (double) numPages;
        for (var screen = 0; screen < numPages; screen++)
        {
            var start = (int) Math.Round(screen * numLettersPerScreen);
            var end = (int) Math.Round((screen + 1) * numLettersPerScreen);
            _pages[screen / 3][screen % 3] = puzzle.EncodedData.Substring(start, end - start);
        }
    }

    protected override int getFontSize(int page, int screen) => 40;
}