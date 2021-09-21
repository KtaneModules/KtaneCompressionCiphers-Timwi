using System;
using System.Collections.Generic;
using System.Linq;
using Rnd = UnityEngine.Random;

namespace CipherModulesLib
{
    public class LempelZivPuzzle
    {
        public string Answer { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool[] Bitmap { get; private set; }
        public List<int> Output { get; private set; }
        public List<int> OutputSymbols { get; private set; }
        public List<(int value, int length)> Dictionary { get; private set; }
        public List<string> EncodedPieces { get; private set; }
        public string EncodedData { get; private set; }
        public string AlphabetName { get; private set; }

        public LempelZivPuzzle(string answer, int width, int height, bool[] bitmap, List<int> output, List<int> outputSymbols, List<(int value, int length)> dictionary, List<string> encodedPieces, string encodedData, string alphabet)
        {
            Answer = answer;
            Width = width;
            Height = height;
            Bitmap = bitmap;
            Output = output;
            OutputSymbols = outputSymbols;
            Dictionary = dictionary;
            EncodedPieces = encodedPieces;
            EncodedData = encodedData;
            AlphabetName = alphabet;
        }

        public static LempelZivPuzzle MakePuzzle(Random rnd = null)
        {
            var data = new Data();

            while (true)
            {
                var word = data.ChooseWord(4, 8, rnd);
                foreach (var alphabet in new[] { Alphabet.Braille, Alphabet.English, Alphabet.Pigpen, Alphabet.Morse, Alphabet.Semaphore }.Shuffle(rnd))
                {
                    var candidatePuzzles = new List<LempelZivPuzzle>();
                    var prevOneLine = false;

                    foreach (var w in Data.Primes)
                    {
                        var writingResult = alphabet.Write(word, w);
                        if (writingResult == null)
                            continue;
                        var (binaryData, h, oneLine) = writingResult.Value;

                        var dictionary = new List<(int value, int length)> { (0, 1), (1, 1) };
                        var symbolLength = 1;
                        var ix = 0;
                        var output = new List<int>();
                        var outputSymbols = new List<int>();
                        while (ix < binaryData.Length)
                        {
                            var symbolIx = dictionary.LastIndexOf(tup => tup.length <= binaryData.Length - ix && Enumerable.Range(ix, tup.length).Aggregate(0, (p, n) => (p << 1) | (binaryData[n] ? 1 : 0)) == tup.value);
                            var (value, length) = dictionary[symbolIx];
                            output.AddRange(Enumerable.Range(0, symbolLength).Select(bit => (symbolIx >> (symbolLength - 1 - bit)) & 1));
                            outputSymbols.Add(symbolIx);
                            if ((dictionary.Count & (dictionary.Count - 1)) == 0)
                                symbolLength++;
                            ix += length;
                            if (ix < binaryData.Length)
                                dictionary.Add(((value << 1) | (binaryData[ix] ? 1 : 0), length + 1));
                        }

                        var encodedData = "";
                        var encodedPieces = new List<string>();
                        var processOutput = output.ToList();
                        while (processOutput.Count > 0)
                        {
                            if (processOutput.Count < 4)
                                goto invalid;

                            if (processOutput[0] != 0 && (processOutput[1] != 0 || processOutput[2] != 0))
                            {
                                encodedData += (char) ('A' + 10 + ((processOutput[0] << 3) | (processOutput[1] << 2) | (processOutput[2] << 1) | (processOutput[3])));
                                encodedPieces.Add(string.Format("{0}={1}", encodedData.Last(), processOutput.Take(4).JoinString()));
                                processOutput.RemoveRange(0, 4);
                            }
                            else
                            {
                                if (processOutput.Count < 5)
                                    goto invalid;

                                encodedData += (char) ('A' + ((processOutput[0] << 4) | (processOutput[1] << 3) | (processOutput[2] << 2) | (processOutput[3] << 1) | (processOutput[4])));
                                encodedPieces.Add(string.Format("{0}={1}", encodedData.Last(), processOutput.Take(5).JoinString()));
                                processOutput.RemoveRange(0, 5);
                            }
                        }
                        if (encodedData.Length <= 25)
                            candidatePuzzles.Add(new LempelZivPuzzle(word, w, h, binaryData, output, outputSymbols, dictionary, encodedPieces, encodedData, alphabet.Name));

                        invalid:
                        if (oneLine)
                        {
                            if (prevOneLine)
                                break;
                            prevOneLine = true;
                        }
                    }

                    if (candidatePuzzles.Count != 0)
                        return candidatePuzzles[rnd == null ? Rnd.Range(0, candidatePuzzles.Count) : rnd.Next(0, candidatePuzzles.Count)];
                }
            }
        }
    }
}
