using System;
using System.Collections.Generic;

namespace CipherModulesLib
{
    public class PokémonSpritePuzzle : BitmapPuzzleBase<PokémonSpritePuzzle>
    {
        public bool[] BitmapAfterXor { get; private set; }

        public PokémonSpritePuzzle(string answer, int width, int height, bool[] bitmap, bool[] bitmapAfterXor, List<bool> output, string encodedData, List<string> encodedPieces, string alphabetName)
            : base(answer, width, height, bitmap, output, encodedData, encodedPieces, alphabetName)
        {
            BitmapAfterXor = bitmapAfterXor;
        }

        public static PokémonSpritePuzzle MakePuzzle(Random rnd = null) => MakePuzzle(rnd, EncodePokémonSprite);

        private static PokémonSpritePuzzle EncodePokémonSprite(bool[] oldBitmapData, string word, Alphabet alphabet, int width)
        {
            // We need the bitmap data to be an even number of bits
            if (oldBitmapData.Length % 2 != 0)
                return null;

            // Step 1: XOR encoding
            var bitmapData = new bool[oldBitmapData.Length];
            for (var i = 0; i < bitmapData.Length; i++)
                bitmapData[i] = i % width == 0 ? oldBitmapData[i] : oldBitmapData[i] ^ oldBitmapData[i - 1];

            // Step 2: RLE
            var isRaw = bitmapData[0] || bitmapData[1];
            var encodedBinary = new List<bool> { isRaw };
            var bitmapIx = 0;

            while (bitmapIx < bitmapData.Length)
            {
                if (isRaw)
                {
                    while (bitmapIx < bitmapData.Length && (bitmapData[bitmapIx] || bitmapData[bitmapIx + 1]))
                    {
                        encodedBinary.Add(bitmapData[bitmapIx]);
                        encodedBinary.Add(bitmapData[bitmapIx + 1]);
                        bitmapIx += 2;
                    }
                    encodedBinary.Add(false);
                    encodedBinary.Add(false);
                }
                else
                {
                    var n = 0;
                    while (bitmapIx < bitmapData.Length && !(bitmapData[bitmapIx] || bitmapData[bitmapIx + 1]))
                    {
                        n++;
                        bitmapIx += 2;
                    }
                    n++;
                    var power = 1;
                    while ((1 << (power + 1)) <= n)
                    {
                        power++;
                        encodedBinary.Add(true);
                    }
                    encodedBinary.Add(false);
                    n -= 1 << power;
                    while (power-- > 0)
                        encodedBinary.Add((n & (1 << power)) != 0);
                }
                isRaw = !isRaw;
            }

            var result = EncodeBinaryToLetters(encodedBinary);
            return result != null && result.Value.encodedData.Length <= 25
                ? new PokémonSpritePuzzle(word, width, bitmapData.Length / width, oldBitmapData, bitmapData, encodedBinary, result.Value.encodedData, result.Value.encodedPieces, alphabet.Name)
                : null;
        }
    }
}
