using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HuffmanCipher
{
    abstract class HuffmanNode
    {
        public int Score { get; private set; }
        public HuffmanNode(int freq) { Score = freq; }
        public abstract IEnumerable<int> EncodeBits(char letter);
        public abstract IEnumerable<int> EncodeTreeStructure();
        public abstract void Populate(string alphabet, ref int ix);
        public abstract SvgInfo GetSvgInfo(ref int depth);
    }

    sealed class HuffmanParentNode : HuffmanNode
    {
        public HuffmanNode Left { get; private set; }
        public HuffmanNode Right { get; private set; }
        public HuffmanParentNode(HuffmanNode left, HuffmanNode right) : base(left.Score + right.Score)
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
        public override SvgInfo GetSvgInfo(ref int depth)
        {
            const double padding = 1;
            var thisDepth = depth++;
            var left = Left.GetSvgInfo(ref depth);
            var right = Right.GetSvgInfo(ref depth);
            double offset = 0;
            for (var i = Math.Min(left.RightWidths.Length, right.LeftWidths.Length) - 1; i >= 0; i--)
                offset = Math.Max(offset, left.RightWidths[i] - right.LeftWidths[i] + padding);
            offset *= .5;
            var newHeight = Math.Max(left.RightWidths.Length, right.LeftWidths.Length);
            double[] newLeft = new double[newHeight + 1], newRight = new double[newHeight + 1];
            newLeft[0] = -1;
            newRight[0] = 1;
            for (var i = 0; i < newHeight; i++)
            {
                newLeft[i + 1] = i >= left.LeftWidths.Length ? right.LeftWidths[i] + offset : left.LeftWidths[i] - offset;
                newRight[i + 1] = i >= right.RightWidths.Length ? left.RightWidths[i] - offset : right.RightWidths[i] + offset;
            }
            return new SvgInfo
            {
                LeftWidths = newLeft,
                RightWidths = newRight,
                Circles = left.Circles.Select(c => c.Move(-offset, "0")).Concat(right.Circles.Select(c => c.Move(offset, "1"))).Concat(new[] { new SvgCircleInfo(0, 0, "node-b", thisDepth) }).ToArray(),
                Paths = left.Paths.Select(c => c.Move(-offset)).Concat(right.Paths.Select(c => c.Move(offset))).Concat(new[] { new SvgPathInfo(offset, thisDepth) }).ToArray(),
                Labels = left.Labels.Select(c => c.Move(-offset)).Concat(right.Labels.Select(c => c.Move(offset))).ToArray(),
            };
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
        public override SvgInfo GetSvgInfo(ref int depth)
        {
            var thisDepth = depth++;
            return new SvgInfo
            {
                LeftWidths = new double[] { -1 },
                RightWidths = new double[] { 1 },
                Circles = new[] { new SvgCircleInfo(0, 0, "node-b", thisDepth) },
                Paths = new SvgPathInfo[0],
                Labels = new[] { new SvgTextInfo(0, 0, Letter.ToString(), thisDepth) }
            };
        }
    }

    sealed class SvgInfo
    {
        public double[] LeftWidths;
        public double[] RightWidths;
        public SvgCircleInfo[] Circles;
        public SvgPathInfo[] Paths;
        public SvgTextInfo[] Labels;

        public string GenerateSubsvg(string topLabel = null, string topHighlight = null, bool useCssClasses = false, bool useDepthInfo = false)
        {
            var alreadyHasTopLabel = topLabel != null && Labels.Any(l => l.Y == 0);
            return string.Format("<g stroke='black' stroke-width='.1'><g fill='none'>{0}</g><g fill='#fff'>{1}</g></g>{2}{3}",
                Paths.Select(p => p.ToSvg(useDepthInfo)).Join(""),
                Circles.Select(c => c.ToSvg(c.Y == 0 ? topHighlight : null, useCssClasses, useDepthInfo)).Join(""),
                Labels.Select(l => l.ToSvg(l.Y == 0 && topLabel != null, useDepthInfo)).Join(""),
                topLabel == null ? null : string.Format("<text x='0' y='{0}' font-size='{1}'>{2}</text>", alreadyHasTopLabel ? .8 : .4, alreadyHasTopLabel ? .5 : 1, topLabel));
        }
    }

    struct SvgPos
    {
        public double X;
        public double Y;
        public SvgPos(double x, double y) { X = x; Y = y; }
        public SvgPos Move(double dx) { return new SvgPos(X + dx, Y + 3); }
    }
    struct SvgCircleInfo
    {
        public SvgPos Pos;
        public double X { get { return Pos.X; } }
        public double Y { get { return Pos.Y; } }
        public string CssClass;
        public int Depth;
        public SvgCircleInfo(double x, double y, string cssClass, int depth) { Pos = new SvgPos(x, y); CssClass = cssClass; Depth = depth; }
        public SvgCircleInfo Move(double dx, string addToClass) { return new SvgCircleInfo(X + dx, Y + 3, CssClass + addToClass, Depth); }
        public string ToSvg(string fill = null, bool useCssClass = false, bool useDepthInfo = false)
        {
            var cssClass = useCssClass || useDepthInfo ? string.Format(" class='{0}'", new[] { useCssClass ? CssClass : null, useDepthInfo ? "node-d" + Depth : null }.Where(s => s != null).Join(" ")) : null;
            return string.Format("<circle cx='{0}' cy='{1}' r='1'{2}{3}/>", X, Y, fill == null ? null : string.Format(" fill='{0}'", fill), cssClass);
        }
    }
    struct SvgPathInfo
    {
        public SvgPos Left;
        public SvgPos Right;
        public SvgPos Parent;
        public int Depth;

        public SvgPathInfo(double offset, int depth)
        {
            Left = new SvgPos(-offset, 3);
            Right = new SvgPos(offset, 3);
            Parent = new SvgPos(0, 0);
            Depth = depth;
        }

        public SvgPathInfo(SvgPos left, SvgPos right, SvgPos parent, int depth) { Left = left; Right = right; Parent = parent; Depth = depth; }
        public SvgPathInfo Move(double dx) { return new SvgPathInfo(Left.Move(dx), Right.Move(dx), Parent.Move(dx), Depth); }
        public string ToSvg(bool useDepthInfo)
        {
            return string.Format("<path d='M{0} {1} {2} {3} {4} {5}'{6} />", Left.X, Left.Y, Parent.X, Parent.Y, Right.X, Right.Y,
                useDepthInfo ? string.Format(" class='node-d{0}'", Depth) : null);
        }
    }
    struct SvgTextInfo
    {
        public SvgPos Pos;
        public double X { get { return Pos.X; } }
        public double Y { get { return Pos.Y; } }
        public string Label;
        public int Depth;
        public SvgTextInfo(double x, double y, string label, int depth) { Pos = new SvgPos(x, y); Label = label; Depth = depth; }
        public SvgTextInfo Move(double dx) { return new SvgTextInfo(X + dx, Y + 3, Label, Depth); }
        public string ToSvg(bool extraLabel, bool useDepthInfo)
        {
            return string.Format("<text x='{0}' y='{1}'{3}{4}>{2}</text>", X, Y + .55 + (extraLabel ? -.3 : 0), Label,
                extraLabel ? " font-size='1.25'" : null, useDepthInfo ? string.Format(" class='node-d{0}'", Depth) : null);
        }
    }
}
