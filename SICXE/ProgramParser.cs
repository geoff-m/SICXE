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
                    if (commentStart > 0)
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

                    if (inst.Operands.Count == 0)
                    {
                        // This is a Format 1 instruction.
                        if (tokens.Length != 1)
                        {
                            Console.WriteLine($"Error {++errorCount}: Instruction {op.ToString()} takes no operands, but \"{tokens[1]}\" was found on line {lineCount} in file {fileName}.");
                            continue;
                        }
                        // Nothing left to do.
                        prog.Add(inst);
                        continue;
                    }
                    if (inst.Operands.Count == 1)
                    {
                        if (tokens.Length != 2)
                        {
                            Console.WriteLine($"Error {++errorCount}: Instruction {op.ToString()} takes no operands, but \"{tokens[1]}\" was found on line {lineCount} in file {fileName}.");
                            continue;
                        }
                        if (!allowedVariableNameRegex.IsMatch(tokens[1]))
                        {
                            Console.WriteLine($"Error {++errorCount}: Invalid symbol name: \"{tokens[1]}\" on line {lineCount} in file {fileName}.");
                            continue;
                        }
                        if (inst.Operands[0].Type == OperandType.Address)
                        {
                            // The instruction exepcts an address.
                            // Check for prefix.
                            switch (tokens[1][0])
                            {
                                case '@':
                                    inst.Operands[0].AddressingMode = AddressingMode.Indirect;
                                    tokens[1] = tokens[1].Substring(1);
                                    break;
                                case '#':
                                    inst.Operands[0].AddressingMode = AddressingMode.Immediate;
                                    tokens[1] = tokens[1].Substring(1);
                                    break;
                                default:
                                    inst.Operands[0].AddressingMode = AddressingMode.Simple;
                                    break;
                            }
                            if (!int.TryParse(tokens[1], out int addr))
                            {
                                Console.WriteLine($"Error {++errorCount}: Instruction {op.ToString()} expects an address, but \"{tokens[1]}\" was found on line {lineCount} in file {fileName}.");
                                continue;
                            }
                            inst.Operands[0].Value = addr;
                            prog.Add(inst);
                            continue;
                        }
                        else
                        {
                            // The instruction expects a register.
                            if (!Enum.TryParse(tokens[1], true, out Register reg))
                            {
                                Console.WriteLine($"Error {++errorCount}: Instruction {op.ToString()} expects a register, but \"{tokens[1]}\" was found on line {lineCount} in file {fileName}.");
                                continue;
                            }
                            inst.Operands[0].Value = (int)reg; // Casting Register to int.
                            prog.Add(inst);
                            continue;
                        }
                    }
                    throw new NotSupportedException("Method only handles operations with 0 or 1 args right now.");
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
