using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace SICXE
{
    class ProgramParser
    {
        public Program ParseProgram(string path)
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

                    // todo: call Line.TryParse()
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
