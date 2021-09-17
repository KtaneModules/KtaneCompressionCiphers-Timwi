using System.Collections.Generic;
using System.Linq;

namespace HuffmanCipher
{
    abstract class HuffmanNode
    {
        public int Score { get; private set; }
        public HuffmanNode(int freq) { Score = freq; }
        public abstract IEnumerable<int> EncodeBits(char letter);
        public abstract IEnumerable<int> EncodeTreeStructure();
        public abstract void Populate(string alphabet, ref int ix);
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
        public override IEnumerable<int> EncodeTreeStructure()
        {
            return new[] { 1 }.Concat(Left.EncodeTreeStructure()).Concat(Right.EncodeTreeStructure());
        }
        public override void Populate(string alphabet, ref int ix)
        {
            Left.Populate(alphabet, ref ix);
            Right.Populate(alphabet, ref ix);
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
        public override IEnumerable<int> EncodeTreeStructure()
        {
            return new[] { 0 };
        }
        public override void Populate(string alphabet, ref int ix)
        {
            Letter = alphabet[ix];
            ix++;
        }
    }
}
