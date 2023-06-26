using System;
using System.Collections.Generic;
using System.Linq;

namespace CompressionCiphersLib
{
    public class LempelZivPuzzle : BitmapPuzzleBase<LempelZivPuzzle>
    {
        public List<int> OutputSymbols { get; private set; }
        public List<(int value, int length)> Dictionary { get; private set; }

        public LempelZivPuzzle(string answer, int width, int height, bool[] bitmap, List<bool> output, List<int> outputSymbols, List<(int value, int length)> dictionary, string encodedData, List<string> encodedPieces, string alphabetName)
            : base(answer, width, height, bitmap, output, encodedData, encodedPieces, alphabetName)
        {
            OutputSymbols = outputSymbols;
            Dictionary = dictionary;
        }

        public static LempelZivPuzzle MakePuzzle(Random rnd = null) => MakePuzzle(rnd, EncodeLempelZiv);

        private static LempelZivPuzzle EncodeLempelZiv(bool[] bitmapData, string word, Alphabet alphabet, int width)
        {
            var dictionary = new List<(int value, int length)> { (0, 1), (1, 1) };
            var symbolLength = 1;
            var bitmapIx = 0;
            var encodedBinary = new List<bool>();
            var outputSymbols = new List<int>();
            while (bitmapIx < bitmapData.Length)
            {
                var symbolIx = dictionary.LastIndexOf(tup => tup.length <= bitmapData.Length - bitmapIx && Enumerable.Range(bitmapIx, tup.length).Aggregate(0, (p, n) => (p << 1) | (bitmapData[n] ? 1 : 0)) == tup.value);
                var (value, length) = dictionary[symbolIx];
                encodedBinary.AddRange(Enumerable.Range(0, symbolLength).Select(bit => ((symbolIx >> (symbolLength - 1 - bit)) & 1) != 0));
                outputSymbols.Add(symbolIx);
                if ((dictionary.Count & (dictionary.Count - 1)) == 0)
                    symbolLength++;
                bitmapIx += length;
                if (bitmapIx < bitmapData.Length)
                    dictionary.Add(((value << 1) | (bitmapData[bitmapIx] ? 1 : 0), length + 1));
            }

            var result = EncodeBinaryToLetters(encodedBinary);
            return result != null && result.Value.encodedData.Length <= 25
                ? new LempelZivPuzzle(word, width, bitmapData.Length / width, bitmapData, encodedBinary, outputSymbols, dictionary, result.Value.encodedData, result.Value.encodedPieces, alphabet.Name)
                : null;
        }
    }
}
