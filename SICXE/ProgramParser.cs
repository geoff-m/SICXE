using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
                    var line = read.ReadLine();
                    ++lineCount;

                    // Strip comments.
                    int commentStart = line.IndexOf(';');
                    if (commentStart > 0)
                        line = line.Substring(0, commentStart);
                    line = line.Trim();
                    if (line.Length == 0)
                        continue;

                    // Parse this line as an instruction.
                    var tokens = SmartSplit(line);
                    if (tokens.Length == 0)
                        continue;
                    Debug.Assert(!tokens.Any(s => s == null || s.Length == 0)); // i don't know if i completely trust that function...
                    InstructionFormat format;
                    AddressingMode mode;
                    if (tokens[0][0] == '+')
                    {
                        // Use format 4 (see p. 59).
                        format = InstructionFormat.Format4;
                        tokens[0] = tokens[0].Substring(1);
                    }
                    if (!Enum.TryParse(tokens[0], true, out Mnemonic op)) // true to ignore case.
                    {
                        Console.WriteLine($"Error {++errorCount}: Unrecognized instruction \"{tokens[0]}\" on line {lineCount} in file {fileName}.");
                        continue;
                    }
                    var inst = new Instruction(op);
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

        private static string[] SmartSplit(string str)
        // Functions same as string.split(), except:
        // Does not split what is surrounded by quotation marks
        // Aware of \" escaped quotation marks.
        // Only splits once on contiguous whitespace.
        {
            if (str.Length == 0)
                return new string[0];
            var ret = new List<string>();
            var current = new StringBuilder();
            bool inwhite = char.IsWhiteSpace(str[0]);
            bool insideliteral = false;
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                if (insideliteral)
                {
                    if (c == '"' && (i > 0 || str[i - 1] != '\\'))
                    {
                        insideliteral = false;
                    }
                    else
                    {
                        //Debug.WriteLine("literal: " + current.ToString());
                    }
                    current.Append(c);
                }
                else
                {
                    if (c == '"' && (i == 0 || str[i - 1] != '\\'))
                    {
                        inwhite = false;
                        insideliteral = true;
                        current.Append(c);
                        continue;
                    }
                    if (char.IsWhiteSpace(c))
                    {
                        if (!inwhite)
                        {
                            // This is a transition to white.
                            //Debug.WriteLine("pushing " + current.ToString());
                            ret.Add(current.ToString());
                            current.Clear();
                            inwhite = true;
                        }
                    }
                    else
                    {
                        inwhite = false;
                        current.Append(c);
                        //Debug.WriteLine("cat name: " + current.ToString());
                    }
                }
            }
            if (!inwhite && current.Length > 0)
            {
                //Debug.WriteLine("pushing " + current.ToString());
                ret.Add(current.ToString());
            }

            return ret.ToArray();
        }
    }
}
