using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;
using MArchiveBatchTool.Psb.Writing;

namespace MArchiveBatchTool.Psb
{
    static class Analysis
    {
        public static void GenerateNameGraphDot(TextWriter writer, PsbReader reader)
        {
            var nodes = reader.GenerateNameNodes();
            writer.WriteLine("digraph {");
            writer.WriteLine("node [shape=record]");
            writer.WriteLine("edge [dir=back]");
            foreach (var node in nodes.OrderBy(x => x.Key))
            {
                node.Value.WriteDot(writer);
            }
            writer.WriteLine("}");
        }

        public static void GenerateNameRanges(TextWriter writer, PsbReader reader)
        {
            var nodes = reader.GenerateNameNodes();
            var root = nodes[0];
            IndentedTextWriter indentedWriter = new IndentedTextWriter(writer);
            WriteRange(indentedWriter, root);
        }

        static void WriteRange(IndentedTextWriter writer, NameNode node)
        {
            RegularNameNode regularNode = node as RegularNameNode;
            if (regularNode != null)
            {
                string line = $"{node.Index} {(char)node.Character} ";
                var regularChildren = regularNode.Children.Values.Where(x => x is RegularNameNode).OrderBy(x => x.Index);
                var terminator = regularNode.Children.Values.Where(x => x is TerminalNameNode).FirstOrDefault();

                if (terminator != null)
                {
                    line += $"[{terminator.Index}] ";
                }

                if (regularChildren.Count() > 0)
                {
                    var minIndex = regularChildren.First().Index;
                    var maxIndex = regularChildren.Last().Index;
                    if (minIndex == maxIndex)
                        line += $"<{minIndex}> ";
                    else
                        line += $"<{minIndex} {maxIndex}> ";
                }

                writer.WriteLine(line.Trim());

                ++writer.Indent;
                foreach (var child in regularNode.Children.Values)
                    WriteRange(writer, child);
                --writer.Indent;
            }
        }

        public static void GenerateRangeUsageVisualization(TextWriter writer, PsbReader reader)
        {
            RangeUsageAnalyzer analyzer = new RangeUsageAnalyzer();
            foreach (var node in reader.GenerateNameNodes().Values)
            {
                RegularNameNode regularNode = node as RegularNameNode;
                if (regularNode != null)
                {
                    var regularChildren = regularNode.Children.Values.Where(x => x is RegularNameNode).OrderBy(x => x.Index);
                    if (regularChildren.Count() > 0)
                    {
                        var minIndex = regularChildren.First().Index;
                        var maxIndex = regularChildren.Last().Index;
                        analyzer.AddRange(node.Index, minIndex, maxIndex, false);
                    }

                    var terminator = regularNode.Children.Values.Where(x => x is TerminalNameNode).FirstOrDefault();
                    if (terminator != null)
                    {
                        analyzer.AddRange(node.Index, terminator.Index, terminator.Index, false);
                    }
                }
                else
                {
                    analyzer.AddRange(node.Index, node.Index, node.Index, true);
                }
            }

            analyzer.OrderNodes();
            analyzer.WriteVisualization(writer);
        }
    }
}
