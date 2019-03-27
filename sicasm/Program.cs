using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SICXEAssembler
{
    /// <summary>
    /// Represents a SIC/XE program that has not been assembled.
    /// </summary>
    public class Program : IList<Line>
    {
        public Program()
        {
            prog = new List<Line>();
        }

        public string OriginFile;

        List<Line> prog;

        private void UpdateLongestLabel()
        {
            LongestLabel = prog.Max(line => line.Label == null ? 0 : line.Label.Length);
        }

        #region List Implementation

        public int Count => ((IList<Line>)prog).Count;

        public bool IsReadOnly => ((IList<Line>)prog).IsReadOnly;

        public Line this[int index]
        {
            get { return ((IList<Line>)prog)[index]; }
            set {
                ((IList<Line>)prog)[index] = value;
                UpdateLongestLabel(); }
        }

        public int IndexOf(Line item)
        {
            return ((IList<Line>)prog).IndexOf(item);
        }

        public void Insert(int index, Line item)
        {
            ((IList<Line>)prog).Insert(index, item);
            UpdateLongestLabel();
        }

        public void RemoveAt(int index)
        {
            ((IList<Line>)prog).RemoveAt(index);
            UpdateLongestLabel();
        }

        public void Add(Line item)
        {
            ((IList<Line>)prog).Add(item);
            UpdateLongestLabel();
        }

        public void Clear()
        {
            ((IList<Line>)prog).Clear();
            UpdateLongestLabel();
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
            bool ret = ((IList<Line>)prog).Remove(item);
            UpdateLongestLabel();
            return ret;
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

        /// <summary>
        /// Gets the length of the longest symbol name in this program. Used for formatting listing files.
        /// </summary>
        public int LongestLabel
        { get; private set; }

        public static bool TryParse(string path, out Program result)
        {
            StreamReader read = null;
            var prog = new Program();
            prog.OriginFile = path;
            int lineCount = 0;
            int errorCount = 0;
            try
            {
                read = new StreamReader(path);
                while (!read.EndOfStream)
                {
                    var textLine = read.ReadLine();
                    ++lineCount;

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

                    Line line;
                    if (Line.TryParse(textLine, out line))
                    {
                        line.LineNumber = lineCount;
                        prog.Add(line);

                        if (line.Label != null && prog.LongestLabel < line.Label.Length)
                            prog.LongestLabel = line.Label.Length;

                        Debug.WriteLine($"Line {line.LineNumber}: {line.ToString()}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Parsing line {lineCount} failed: \"{textLine}\"");
                        ++errorCount;
                    }
                }
            }
            finally
            {
                if (read != null)
                    read.Dispose();
            }
            Console.Error.WriteLine("\nParse completed with {0} {1}.",
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
