using System;
using System.Collections.Generic;
using System.Linq;
using Rnd = UnityEngine.Random;

namespace CipherModulesLib
{
    public abstract class BitmapPuzzleBase<TPuzzle>
    {
        public string Answer { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool[] Bitmap { get; private set; }
        public List<bool> Output { get; private set; }
        public string EncodedData { get; private set; }
        public List<string> EncodedPieces { get; private set; }
        public string AlphabetName { get; private set; }

        public BitmapPuzzleBase(string answer, int width, int height, bool[] bitmap, List<bool> output, string encodedData, List<string> encodedPieces, string alphabetName)
        {
            Answer = answer;
            Width = width;
            Height = height;
            Bitmap = bitmap;
            Output = output;
            EncodedData = encodedData;
            EncodedPieces = encodedPieces;
            AlphabetName = alphabetName;
        }

        internal static (string encodedData, List<string> encodedPieces)? EncodeBinaryToLetters(List<bool> binary)
        {
            var encodedData = "";
            var encodedPieces = new List<string>();
            var processOutput = binary.ToList();
            while (processOutput.Count > 0)
            {
                if (processOutput.Count < 4)
                    return null;

                if (processOutput[0] && (processOutput[1] || processOutput[2]))
                {
                    encodedData += (char) ('A' + 10 + ((processOutput[0] ? 8 : 0) | (processOutput[1] ? 4 : 0) | (processOutput[2] ? 2 : 0) | (processOutput[3] ? 1 : 0)));
                    encodedPieces.Add(string.Format("{0}={1}", encodedData.Last(), processOutput.Take(4).Select(b => b ? "1" : "0").JoinString()));
                    processOutput.RemoveRange(0, 4);
                }
                else
                {
                    if (processOutput.Count < 5)
                        return null;

                    encodedData += (char) ('A' + ((processOutput[0] ? 16 : 0) | (processOutput[1] ? 8 : 0) | (processOutput[2] ? 4 : 0) | (processOutput[3] ? 2 : 0) | (processOutput[4] ? 1 : 0)));
                    encodedPieces.Add(string.Format("{0}={1}", encodedData.Last(), processOutput.Take(5).Select(b => b ? "1" : "0").JoinString()));
                    processOutput.RemoveRange(0, 5);
                }
            }
            return (encodedData, encodedPieces);
        }

        internal static TPuzzle MakePuzzle(Random rnd, Func<bool[], string, Alphabet, int, TPuzzle> encode)
        {
            var data = new Data();

            while (true)
            {
                var word = data.ChooseWord(4, 8, rnd);
                foreach (var alphabet in new[] { Alphabet.Braille, Alphabet.English, Alphabet.Pigpen, Alphabet.Morse, Alphabet.Semaphore }.Shuffle(rnd))
                {
                    var candidatePuzzles = new List<TPuzzle>();
                    var prevOneLine = false;

                    foreach (var width in Data.Primes)
                    {
                        var writingResult = alphabet.Write(word, width);
                        if (writingResult == null)
                            continue;
                        var (bitmapData, h, oneLine) = writingResult.Value;

                        var result = encode(bitmapData, word, alphabet, width);
                        if (result != null)
                            candidatePuzzles.Add(result);

                        if (oneLine && prevOneLine)
                            break;
                        prevOneLine = oneLine;
                    }

                    if (candidatePuzzles.Count != 0)
                        return candidatePuzzles[rnd == null ? Rnd.Range(0, candidatePuzzles.Count) : rnd.Next(0, candidatePuzzles.Count)];
                }
            }
        }
    }
}