using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICXE
{
    class Assembler
    {
        public static bool TryAssemble(Program prog, out Word[] result)
        {
            throw new NotImplementedException();
        }

        public bool TryAssemble(out Word[] result)
        {
            if (!PassOne())
            {
                Console.WriteLine("Pass one failed.");
                result = null;
                return false;
            }
            Console.WriteLine("Pass one succeeded.");

            result = null;
            return false;
        }

        Program prog;
        public Assembler(Program p)
        {
            prog = p;
        }

        /// <summary>
        /// The binary segment associated with each line in the program.
        /// </summary>
        private IList<byte>[] bytes;

        Dictionary<string, Symbol> symbols;
        bool donePassOne = false;
        /// <summary>
        /// Takes account of all symbols declared or referenced, and computes the total length of the assembled binary.
        /// </summary>
        /// <returns></returns>
        private bool PassOne()
        {
            if (donePassOne)
            {
                throw new InvalidOperationException("Pass one has already been done!");
            }

            bytes = new List<byte>[prog.Count];
            symbols = new Dictionary<string, Symbol>();
            for (int lineIdx = 0; lineIdx < prog.Count; ++lineIdx)
            {
                Line line = prog[lineIdx];
                if (line is AssemblerDirective dir)
                {
                    if (dir.Value == null)
                    {
                        Console.WriteLine($"Incomplete symbol declaration is not allowed: {line.ToString()}");
                        return false;
                    }
                    int val;
                    switch (dir.Directive)
                    {
                        // We require these directives to have an integer as their argument.
                        case AssemblerDirective.Mnemonic.BYTE:
                            if (int.TryParse(dir.Value, out val))
                            {
                                if (line.Label != null)
                                    if (!SetSymbolValue(line.Label, val))
                                        return false;
                                bytes[lineIdx] = EncodeTwosComplement(val, 8);
                            }
                            else
                            {
                                // todo: some byte directives don't have the form of an integer. handle these.
                                Console.WriteLine($"Could not parse byte \"{dir.Value}\" in \"{dir.ToString()}\"");
                                return false;
                            }
                            break;
                        case AssemblerDirective.Mnemonic.WORD:
                            if (int.TryParse(dir.Value, out val))
                            {
                                if (line.Label != null)
                                    if (!SetSymbolValue(line.Label, val))
                                        return false;
                                bytes[lineIdx] = EncodeTwosComplement(val, 12);
                            }
                            else
                            {
                                // todo: some word directives don't have the form of an integer. handle these.
                                Console.WriteLine($"Could not parse word \"{dir.Value}\" in \"{dir.ToString()}\"");
                                return false;
                            }
                            break;
                        case AssemblerDirective.Mnemonic.RESW:
                            if (int.TryParse(dir.Value, out val))
                            {
                                if (line.Label != null)
                                    if (!SetSymbolValue(line.Label, val))
                                        return false;
                                
                                // todo: assemble the directive.

                            }
                            else
                            {
                                // todo: some word/byte directives don't have the form of an integer. handle these.
                                Console.WriteLine($"Could not parse integer \"{dir.Value}\" in \"{dir.ToString()}\"");
                                return false;
                            }
                            break;
                        // We allow this directive to have either an integer or a symbol as its argument.
                        case AssemblerDirective.Mnemonic.START:
                        case AssemblerDirective.Mnemonic.END:
                            if (dir.Value == null)
                            {
                                Console.WriteLine("START and END directives must be followed by a label or address!");
                                return false;
                            }
                            if (int.TryParse(dir.Value, out val))
                            {
                                if (line.Label != null)
                                    if (!CreateSymbol(line.Label))
                                        return false;
                            }
                            else
                            {
                                TouchSymbol(dir.Value);
                            }
                            break;
                    }
                }
                else // The line must be an instruction.
                {
                    if (line.Label != null)
                    {
                        if (!CreateSymbol(line.Label))
                            return false;
                    }

                    var instr = (Instruction)line;
                    var operands = instr.Operands;
                    switch (instr.Format)
                    {
                        case InstructionFormat.Format1:
                            if (operands.Count > 0)
                            {
                                Console.WriteLine($"Error: Format 1 instruction takes no operands, but {string.Join(", ", operands)} was given!");
                                return false;
                            }
                            bytes[lineIdx] = new byte[] { (byte)instr.Operation };
                            break;

                        case InstructionFormat.Format2:
                            if (operands.Count == 0)
                            {
                                Console.WriteLine($"Error: Format 2 instruction takes 1 or 2 operands, but none were given!");
                                return false;
                            }
                            if (operands.Count > 2)
                            {
                                Console.WriteLine($"Error: Format 2 instruction takes 1 or 2 operands, but {string.Join(", ", operands)} was given!");
                                return false;
                            }
                            
                            break;
                    }



                    if (operands.Count == 1)
                    {
                        var operandSymbol = operands[0].Symbol;
                        if (operandSymbol != null)
                            TouchSymbol(operandSymbol);
                    }

                    

                    
                }
            }

            donePassOne = true;
            return true;
        }

        /// <summary>
        /// Creates the symbol with the specified name if it does not exist.
        /// </summary>
        /// <param name="name"></param>
        private void TouchSymbol(string name)
        {
            if (!symbols.ContainsKey(name))
                symbols.Add(name, new Symbol(name));
        }

        /// <summary>
        /// Creates a symbol, ensuring it does not already exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if the symbol was created. False if it already exists.</returns>
        private bool CreateSymbol(string name)
        {
            if (symbols.ContainsKey(name))
            {
                Console.WriteLine($"Multiple declarations of label \"{name}\"!");
                return false;
            }
            var newSymbol = new Symbol(name);

            symbols[name] = newSymbol;
            return true;
        }

        /// <summary>
        /// Sets a symbol's value, ensuring it does not already have one. If the symbol with the specified name does not exist, it will be created.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool SetSymbolValue(string name, int value)
        {
            if (symbols.TryGetValue(name, out Symbol existing))
            {
                if (existing.Value.HasValue)
                {
                    Console.WriteLine($"Symbol \"{name}\" already has value {existing.Value}!");
                    return false;
                }
                existing.Value = value;
            }
            symbols.Add(name, new Symbol(name) { Value = value });
            return true;
        }

        private static byte[] EncodeTwosComplement(int n, int bits) // untested.
        {
            int highMask = checked(~((1 << bits) - 1));
            if ((highMask & bits) > 0)
            {
                throw new OverflowException();
            }

            if (n < 0)
            {
                n = ~n;
                n = checked(n + 1);
                n &= ~highMask;
            }

            byte high = (byte)((n & 0xff0000) >> 16);
            byte middle = (byte)((n & 0xff00) >> 8);
            byte low = (byte)(n & 0xff);
            if (high == 0)
            {
                if (middle == 0)
                {
                    return new byte[] { low };
                }
                return new byte[] { low, middle };
            }
            return new byte[] { low, middle, high };

        }
    }
}
