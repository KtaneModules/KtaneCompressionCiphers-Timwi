using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CipherModulesLib
{
    public abstract class HuffmanModuleBase : CipherModuleBase
    {
        internal string generateStepSvgNoScore(HuffmanNode node, bool useDepthInfo = false)
        {
            return generateStepSvg(new[] { node }, null, null, true, useDepthInfo: useDepthInfo);
        }

        internal string generateStepSvg(IEnumerable<HuffmanNode> nodes, HuffmanNode lowestScoreNode, HuffmanNode secondLowestScoreNode,
            bool useCssClasses = false, bool showScore = false, bool useDepthInfo = false)
        {
            const double padding = 1;
            const double yPadding = .5;

            var output = new StringBuilder();
            List<double[]> allRightWidths = new List<double[]>();
            double yOffset = 0;
            foreach (var node in nodes)
            {
                var depth = 0;
                var inf = node.GetSvgInfo(ref depth);
                double offset = 0;

                if (allRightWidths.Count == 0)
                {
                    offset = -inf.LeftWidths.Min();
                    allRightWidths.Add(inf.RightWidths.Select(rw => rw + offset).ToArray());
                }
                else
                {
                    var rightWidths = allRightWidths.Last();
                    for (var i = Math.Min(rightWidths.Length, inf.LeftWidths.Length) - 1; i >= 0; i--)
                        offset = Math.Max(offset, rightWidths[i] - inf.LeftWidths[i] + padding);
                    var newRightWidths = new double[Math.Max(rightWidths.Length, inf.RightWidths.Length)];
                    for (var i = 0; i < newRightWidths.Length; i++)
                        newRightWidths[i] = i >= inf.RightWidths.Length ? rightWidths[i] : inf.RightWidths[i] + offset;

                    if (newRightWidths.Max() > 40)
                    {
                        yOffset += 3 * rightWidths.Length + 1;
                        offset = -inf.LeftWidths.Min();
                        allRightWidths.Add(inf.RightWidths.Select(rw => rw + offset).ToArray());
                    }
                    else
                        allRightWidths[allRightWidths.Count - 1] = newRightWidths;
                }

                output.AppendFormat("<g transform='translate({0} {1})'>{2}</g>", offset, yOffset,
                    inf.GenerateSubsvg(showScore ? node.Score.ToString() : null, node == lowestScoreNode ? "#bbb" : node == secondLowestScoreNode ? "#ddd" : null, useCssClasses, useDepthInfo));
            }
            return string.Format("<svg xmlns='http://www.w3.org/2000/svg' viewBox='{1} {2} {3} {4}' text-anchor='middle' font-size='1.5'>{0}</svg>",
                output,
                -padding,
                -1 - yPadding,
                allRightWidths.Max(r => r.Max()) + 2 * padding,
                yOffset + 3 * allRightWidths.Last().Length + 2 + 2 * yPadding);
        }
    }
}
