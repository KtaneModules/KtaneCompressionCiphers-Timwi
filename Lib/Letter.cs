using System;
using System.Collections.Generic;
using System.Linq;

namespace CompressionCiphersLib
{
    sealed class Letter
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public string Value { get; private set; }
        public bool CanBeInitial { get; private set; }
        public bool CanBeFinal { get; private set; }
        private readonly bool[] _pixels;
        public bool Get(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height && _pixels[x + Width * y];
        public Letter(string value, int w, int h, bool[] pixels, bool canBeInitial = true, bool canBeFinal = true)
        {
            if (pixels == null || w <= 0 || h <= 0 || pixels.Length != w * h)
                throw new InvalidOperationException();
            Value = value;
            Width = w;
            Height = h;
            _pixels = pixels.ToArray();
            CanBeInitial = canBeInitial;
            CanBeFinal = canBeFinal;
        }
        public void Write(HashSet<int> into, int intoWidth, int x, int y)
        {
            if (x < 0 || y < 0 || x + Width > intoWidth)
                throw new InvalidOperationException();
            for (var i = 0; i < _pixels.Length; i++)
                if (_pixels[i])
                    into.Add(x + (i % Width) + intoWidth * (y + (i / Width)));
        }
    }
}