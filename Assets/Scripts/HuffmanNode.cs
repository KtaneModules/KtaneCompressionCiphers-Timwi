using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace HuffmanCipher
{
    abstract class HuffmanNode
    {
        public int Score { get; private set; }
        public HuffmanNode(int freq) { Score = freq; }
        public abstract IEnumerable<int> EncodeBits(char letter);
    }

    sealed class HuffmanBranch : HuffmanNode
    {
        public HuffmanNode Left { get; private set; }
        public HuffmanNode Right { get; private set; }
        public HuffmanBranch(HuffmanNode left, HuffmanNode right) : base(left.Score + right.Score)
        {
            Left = left;
            Right = right;
        }
        public override IEnumerable<int> EncodeBits(char letter)
        {
            var left = Left.EncodeBits(letter);
            if (left != null)
                return new[] { 0 }.Concat(left);
            var right = Right.EncodeBits(letter);
            if (right != null)
                return new[] { 1 }.Concat(right);
            return null;
        }
        public override string ToString()
        {
            return string.Format("[{0}{1}]", Left, Right);
        }
    }

    sealed class HuffmanLeaf : HuffmanNode
    {
        public char Letter { get; private set; }
        public HuffmanLeaf(int freq, char letter) : base(freq) { Letter = letter; }
        public override IEnumerable<int> EncodeBits(char letter)
        {
            return letter == Letter ? new int[0] : null;
        }
        public override string ToString()
        {
            return Letter.ToString();
        }
    }
}
