using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SICXE
{
    /// <summary>
    /// Represents a SIC/XE program that has not been assembled.
    /// </summary>
    class Program : IList<Line>
    {
        public Program()
        {
            prog = new List<Line>();
        }

        List<Line> prog;

        #region List Implementation

        public int Count => ((IList<Line>)prog).Count;

        public bool IsReadOnly => ((IList<Line>)prog).IsReadOnly;

        public Line this[int index] { get => ((IList<Line>)prog)[index]; set => ((IList<Line>)prog)[index] = value; }

        public int IndexOf(Line item)
        {
            return ((IList<Line>)prog).IndexOf(item);
        }

        public void Insert(int index, Line item)
        {
            ((IList<Line>)prog).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<Line>)prog).RemoveAt(index);
        }

        public void Add(Line item)
        {
            ((IList<Line>)prog).Add(item);
        }

        public void Clear()
        {
            ((IList<Line>)prog).Clear();
        }

        public bool Contains(Line item)
        {
            return ((IList<Line>)prog).Contains(item);
        }

        public void CopyTo(Line[] array, int arrayIndex)
        {
            ((IList<Line>)prog).CopyTo(array, arrayIndex);
        }

        public bool Remove(Line item)
        {
            return ((IList<Line>)prog).Remove(item);
        }

        public IEnumerator<Line> GetEnumerator()
        {
            return ((IList<Line>)prog).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<Line>)prog).GetEnumerator();
        }
        #endregion

        public static Program Parse(string path)
        {
            StreamReader read = null;
            var prog = new Program();
            var allowedVariableNameRegex = new Regex("\\w");
            int lineCount = 0;
            int errorCount = 0;
            try
            {
                read = new StreamReader(path);
                var fileName = Path.GetFileName(path);
                while (!read.EndOfStream)
                {
                    var textLine = read.ReadLine();
                    ++lineCount;

                    // Strip comments.
                    int commentStart = textLine.IndexOf('.');
                    if (commentStart >= 0)
                        textLine = textLine.Substring(0, commentStart);
                    textLine = textLine.Trim();
                    if (textLine.Length == 0)
                        continue;

                    // Parse this line.
                    var tokens = textLine.SmartSplit();

                    if (tokens.Length == 0)
                        continue;
                    Debug.Assert(!tokens.Any(s => s == null || s.Length == 0));

                    // Format of a line: {label} [operation] {operand} {comment, possibly multiple tokens}
                    // To determine whether the first token is a label or an operation, we'll just try assuming it is an operation.
                    // If that assumption fails (no operation can be parsed), we'll assume it's a label.
                    // As a consequence, operation mnemonics will never be valid as labels.

                    if (Line.TryParse(textLine, out Line line))
                    {
                        prog.Add(line);
                    }
                    else
                    {
                        Console.WriteLine($"Parsing line {lineCount} failed: \"{textLine}\"");
                    }
                }
            }
            finally
            {
                if (read != null)
                    read.Dispose();
            }
            Console.WriteLine("Parse completed with {0} {1}.",
                errorCount,
                errorCount == 1 ? "error" : "errors");
            return prog;
        }
    }
}
