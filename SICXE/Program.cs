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

        public static bool TryParse(string path, out Program result)
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
                    textLine = textLine.TrimEnd();
                    if (textLine.Length == 0)
                        continue; // Ignore line because it is empty.
                    if (textLine[0] == '.')
                        continue; // Ignore line because it is a comment.

                    // Parse this line.
                    var tokens = textLine.SmartSplit();
                    if (tokens.Length == 0)
                        continue;
                    Debug.Assert(!tokens.Any(s => s == null || s.Length == 0));

                    // Format of a line: {label} [operation] {operand} {comment, possibly multiple tokens}
                    // Whitespace before operation is required.
                    // Whitespace before oprenand is required, if operand is present.

                    if (Line.TryParse(textLine, out Line line))
                    {
                        prog.Add(line);
                        Debug.WriteLine(line.ToString());
                    }
                    else
                    {
                        Console.WriteLine($"Parsing line {lineCount} failed: \"{textLine}\"");
                        ++errorCount;
                    }
                }
            }
            finally
            {
                if (read != null)
                    read.Dispose();
            }
            Console.WriteLine("\nParse completed with {0} {1}.\n",
                errorCount,
                errorCount == 1 ? "error" : "errors");
            if (errorCount == 0)
            {
                result = prog;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }
    }
}
