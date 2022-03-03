using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CipherModulesLib
{
    sealed class Alphabet
    {
        private readonly Letter[] _letters;
        public string Name { get; private set; }
        public bool AllowsWrap { get; private set; }
        public Alphabet(string name, bool allowsWrap, Letter[] letters)
        {
            Name = name;
            AllowsWrap = allowsWrap;
            _letters = letters?.ToArray() ?? throw new ArgumentNullException(nameof(letters));
        }

        public (bool[] binaryData, int height, bool oneLine)? Write(string text, int w)
        {
            var result = new HashSet<int>();
            int x = 0, y = 0, h = 0, i = 0;
            while (i < text.Length)
            {
                var letter = _letters.Where(l => i + l.Value.Length <= text.Length && text.Substring(i, l.Value.Length) == l.Value && (i > 0 || l.CanBeInitial) && (i + l.Value.Length < text.Length || l.CanBeFinal)).MaxElementOrDefault(e => e.Value.Length);
                if (letter == null)
                    return null;
                if (x + letter.Width > w)
                {
                    if (!AllowsWrap)
                        return null;
                    x = 0;
                    y = h;
                }
                if (x == 0 && letter.Width > w)
                    return null;
                letter.Write(result, w, x, y);
                x += letter.Width;
                h = Math.Max(h, y + letter.Height);
                i += letter.Value.Length;
            }
            return (Enumerable.Range(0, w * Data.Primes.First(p => p >= h)).Select(result.Contains).Concat(new[] { w > h }).ToArray(), h, y == 0);
        }

        public static Alphabet English { get; private set; }
        public static Alphabet Pigpen { get; private set; }
        public static Alphabet Morse { get; private set; }
        public static Alphabet Braille { get; private set; }
        public static Alphabet Semaphore { get; private set; }

        static Alphabet()
        {
            var alphabet = Ut.NewArray(
                new[] { "█░", "█░░", "░░", "░░█", "██", "░█", "██", "█░░", "█", "░█", "█░░", "█░", "░░░░░", "░░░", "░░░", "██░", "░██", "░░", "░█", "█░", "░░░", "░░░", "░░░░░", "░░░", "█░█", "██" },
                new[] { "░█", "██░", "██", "░██", "██", "█░", "██", "██░", "░", "░░", "█░█", "█░", "██░█░", "██░", "░█░", "█░█", "█░█", "██", "█░", "██", "█░█", "█░█", "█░░░█", "█░█", "░██", "░█" },
                new[] { "██", "█░█", "█░", "█░█", "█░", "██", "░█", "█░█", "█", "░█", "██░", "█░", "█░█░█", "█░█", "█░█", "██░", "░██", "█░", "░█", "█░", "█░█", "█░█", "█░█░█", "░█░", "░░█", "█░" },
                new[] { "██", "██░", "██", "░██", "░█", "█░", "█░", "█░█", "█", "██", "█░█", "░█", "█░█░█", "█░█", "░█░", "█░░", "░░█", "█░", "█░", "░█", "░██", "░█░", "░█░█░", "█░█", "██░", "██" });
            English = new Alphabet("English", true, Enumerable.Range(0, 26).Select(i => new Letter(((char) ('A' + i)).ToString(), alphabet[0][i].Length, alphabet.Length, alphabet.SelectMany(row => row[i].Select(ch => ch == '█')).ToArray())).ToArray());

            alphabet = Ut.NewArray(
                new[] { "░░█", "█░█", "█░░", "███", "███", "███", "███", "███", "███", "░░█", "█░█", "█░░", "███", "███", "███", "███", "███", "███", "░░░", "░█░", "░█░", "░█░", "░░░", "░█░", "░█░", "░█░" },
                new[] { "░░█", "█░█", "█░░", "░░█", "█░█", "█░░", "░░█", "█░█", "█░░", "░██", "███", "██░", "░██", "███", "██░", "░██", "███", "██░", "█░█", "░░█", "█░░", "█░█", "███", "░██", "██░", "███" },
                new[] { "███", "███", "███", "███", "███", "███", "░░█", "█░█", "█░░", "███", "███", "███", "███", "███", "███", "░░█", "█░█", "█░░", "░█░", "░█░", "░█░", "░░░", "░█░", "░█░", "░█░", "░░░" });
            Pigpen = new Alphabet("Pigpen", true, Enumerable.Range(0, 26).Select(i => new Letter(((char) ('A' + i)).ToString(), alphabet[0][i].Length, alphabet.Length, alphabet.SelectMany(row => row[i].Select(ch => ch == '█')).ToArray())).ToArray());

            Braille = new Alphabet("Braille", true, @"
A=1 B=12 C=14 D=145 E=15 F=124 G=1245 H=125 I=24 J=245
K=13 L=123 M=134 N=1345 O=135 P=1234 Q=12345 R=1235 S=234 T=2345 U=136 V=1236 W=2456 X=1346 Y=13456 Z=1356
AND=12346 FOR=123456 THE=2346 WITH=23456
AR=345 -BB-=23 -CC-=25 CH=16 -EA-=2 ED=1246 EN=26 ER=12456 -FF-=235 -GG-=2356
GH=126 IN=35 -ING=346 OF=12356 OU=1256 OW=246 SH=146 ST=34 TH=1456 WH=156"
                .Trim()
                .Split(new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(bit => Regex.Match(bit, @"^(-)?(\w+)(-)?=(\d+)$"))
                .Where(m => m.Success)
                .Select(m => new Letter(m.Groups[2].Value, 2, 3, Enumerable.Range(0, 6).Select(ix => m.Groups[4].Value.Contains((char) ('1' + ix / 2 + 3 * (ix % 2)))).ToArray(), !m.Groups[1].Success, !m.Groups[3].Success))
                .ToArray());

            alphabet = Ut.NewArray(
                    new[] { "░░░", "░░░", "█░░", "░█░", "░░█", "░░░", "░░░", "░░░", "█░░", "░█░", "░█░", "░░█", "░░░", "░░░", "█░░", "░█░", "░░█", "░░░", "░░░", "██░", "█░█", "░█░", "░░█", "░░█", "█░░", "░░░" },
                    new[] { "░█░", "██░", "░█░", "░█░", "░█░", "░██", "░█░", "██░", "░█░", "░██", "░█░", "░█░", "░██", "░█░", "██░", "██░", "██░", "███", "██░", "░█░", "░█░", "░█░", "░██", "░█░", "░██", "░██" },
                    new[] { "██░", "░█░", "░█░", "░█░", "░█░", "░█░", "░██", "█░░", "█░░", "░░░", "█░░", "█░░", "█░░", "█░█", "░░░", "░░░", "░░░", "░░░", "░░█", "░░░", "░░░", "░░█", "░░░", "░░█", "░░░", "░░█" });
            Semaphore = new Alphabet("Semaphore", true, Enumerable.Range(0, 26).Select(i => new Letter(((char) ('A' + i)).ToString(), alphabet[0][i].Length, alphabet.Length, alphabet.SelectMany(row => row[i].Select(ch => ch == '█')).ToArray())).ToArray());

            var morse = ".-|-...|-.-.|-..|.|..-.|--.|....|..|.---|-.-|.-..|--|-.|---|.--.|--.-|.-.|...|-|..-|...-|.--|-..-|-.--|--..".Split('|');
            Morse = new Alphabet("Morse", false, Enumerable.Range(0, 26).Select(i =>
            {
                var str = morse[i].Replace(".", "# ").Replace("-", "### ").Trim();
                return new Letter(((char) ('A' + i)).ToString(), 1, str.Length, str.Select(ch => ch == '#').ToArray());
            }).ToArray());
        }
    }
}