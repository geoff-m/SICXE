using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SICXE
{
    class Assembler
    {
        // This is the only public methods in the class at this point.
        public static bool TryAssemble(Program prog, out Binary result)
        {
            var inst = new Assembler(prog);
            if (!inst.PassOne())
            {
                Console.WriteLine("Pass one failed.");
                result = null;
                return false;
            }
            Console.WriteLine("Pass one succeeded.");

            if (!inst.PassTwo())
            {
                Console.WriteLine("Pass two failed.");
                result = null;
                return false;
            }
            Console.WriteLine("Pass two succeeded.");

            inst.outputBinary.AddSegment(inst.codeSegment);

            result = inst.outputBinary;
            return true;
        }


        Program prog;
        Binary outputBinary;
        private Assembler(Program p)
        {
            prog = p;
            outputBinary = new Binary();
        }

        /// <summary>
        /// The current binary segment.
        /// </summary>
        private Segment currentSegment;

        /// <summary>
        /// The segment what contains code.
        /// </summary>
        private Segment codeSegment;

        /// <summary>
        /// This points to somewhere in 'codeSegment'.
        /// </summary>
        Symbol entryPoint = null;

        bool hitStart = false; // We allow only one of these.
        bool hitEnd = false; // We allow only one of these.

        /// <summary>
        /// The total number of instruction bytes in the program.
        /// </summary>
        int instructionBytes = 0;

        Dictionary<string, Symbol> symbols;
        bool donePassOne = false;
        /// <summary>
        /// Takes account of all symbols declared or referenced, and computes the total length of the assembled binary.
        /// </summary>
        /// <returns>True if assembly can continue. False on failure.</returns>
        private bool PassOne()
        {
            if (donePassOne)
            {
                // This indicates a bug.
                throw new InvalidOperationException("Pass one has already been done!");
            }

            int bytesSoFar = 0;
            var binary = new byte[prog.Count][];
            symbols = new Dictionary<string, Symbol>();
            for (int lineIdx = 0; lineIdx < prog.Count; ++lineIdx)
            {
                Line line = prog[lineIdx];
                string label;
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
                            // for now we allow only exactly 1 byte.
                            if (int.TryParse(dir.Value, out val) && (val <= 255 || val >= -127))
                            {
                                label = line.Label;
                                if (label != null)
                                {
                                    if (!SetSymbolValue(label, val))
                                        return false;
                                    symbols[label].Address = bytesSoFar;

                                }
                                line.Address = bytesSoFar;
                                binary[lineIdx] = EncodeTwosComplement(val, 8);
                                bytesSoFar += 1;
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
                                label = line.Label;
                                if (label != null)
                                {
                                    if (!SetSymbolValue(label, val))
                                        return false;

                                    symbols[label].Address = bytesSoFar;
                                }
                                line.Address = bytesSoFar;
                                binary[lineIdx] = EncodeTwosComplement(val, 12);
                                bytesSoFar += Word.Size;
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
                                label = line.Label;
                                if (label != null)
                                    TouchSymbol(label);

                                symbols[label].Address = bytesSoFar;
                                line.Address = bytesSoFar;
                                bytesSoFar += Word.Size * val;
                            }
                            else
                            {
                                Console.WriteLine($"Could not parse integer \"{dir.Value}\" in \"{dir.ToString()}\"");
                                return false;
                            }
                            break;
                        case AssemblerDirective.Mnemonic.START:
                            if (hitStart)
                            {
                                Console.WriteLine("Multiple START directives are not allowed.");
                                return false;
                            }

                            hitStart = true;
                            if (dir.Value == null)
                            {
                                Console.WriteLine("START directive must be followed by an address!");
                                return false;
                            }
                            if (int.TryParse(dir.Value, out val))
                            {
                                if (currentSegment == null)
                                {
                                    currentSegment = new Segment
                                    {
                                        BaseAddress = val
                                    };
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Cannot parse start address \"{dir.Value}\".");
                                return false;
                            }
                            break;
                        case AssemblerDirective.Mnemonic.END:
                            if (hitEnd)
                            {
                                Console.WriteLine("Multiple END directives are not allowed.");
                                return false;
                            }
                            hitEnd = true;

                            if (dir.Value == null)
                            {
                                Console.WriteLine("END directive must be followed by a label or address!");
                                return false;
                            }
                            if (int.TryParse(dir.Value, out val))
                            {
                                if (line.Label != null)
                                    if (!CreateSymbol(line.Label))
                                        return false;
                                outputBinary.EntryPoint = val;
                            }
                            else
                            {
                                TouchSymbol(dir.Value);
                                entryPoint = symbols[dir.Value];
                            }
                            break;
                    }
                }
                else // The line must be an instruction.
                {
                    label = line.Label;
                    if (label != null)
                    {
                        label = TrimIndexer(label);
                        if (!CreateSymbol(label))
                            return false;
                        symbols[label].Address = bytesSoFar;
                    }
                    line.Address = bytesSoFar;
                    var instr = (Instruction)line;
                    bytesSoFar += (int)instr.Format;
                    instructionBytes += (int)instr.Format;
                }
            }

            donePassOne = true;
            return true;
        }

        int @base; // todo: implement base directive.
        bool donePassTwo = false;
        // Generates displacements.
        private bool PassTwo()
        {
            if (donePassTwo)
            {
                // This indicates a bug.
                throw new InvalidOperationException("Pass two has already been done!");
            }
            Debug.Assert(codeSegment == null);
            codeSegment = new Segment
            {
                Data = new byte[instructionBytes]
            };

            int ip = 0; // Index in the code segment.
            byte[] binInstr = null;
            var instructions = prog.Where(l => l is Instruction).Cast<Instruction>().ToList();
            for (int instrIdx = 0; instrIdx < instructions.Count; ++instrIdx)
            {
                Instruction instr = instructions[instrIdx];
                for (int operandIdx = 0; operandIdx < instr.Operands.Count; ++operandIdx)
                {
                    Operand operand = instr.Operands[operandIdx];
                    if (!operand.Value.HasValue)
                    {
                        string sym = TrimIndexer(operand.SymbolName);
                        Debug.Assert(sym != null);
                        operand.Value = symbols[sym].Address;
                    }
                }
                var operands = instr.Operands;

                switch (instr.Format)
                {
                    case InstructionFormat.Format1:
                        Debug.Assert(operands.Count == 0, $"Error: Format 1 instruction takes no operands, but {string.Join(", ", operands)} was given!");
                        binInstr = new byte[] { (byte)instr.Operation };
                        break;
                    case InstructionFormat.Format2:
                        Debug.Assert(operands.Count == 1 || operands.Count == 2, $"Format 2 instruction takes 1 or 2 operands, but {string.Join(", ", operands)} was given!");
                        binInstr = AssembleFormat2(instr);
                        break;
                    case InstructionFormat.Format3:
                    case InstructionFormat.Format4:
                        binInstr = AssembleFormats34(instr, instr.Address.Value + (int)instr.Format, @base);
                        break;
                    default:
                        // This indicates a bug.
                        throw new ArgumentException($"Instruction has a bad format.");
                }
                Array.Copy(binInstr, 0, codeSegment.Data, ip, binInstr.Length);
                ip += binInstr.Length;
            }

            if (entryPoint != null)
            {
                outputBinary.EntryPoint = entryPoint.Address.Value;
            }
            else
            {
                Console.WriteLine($"Warning: No END directive. Assuming entry point is {codeSegment.BaseAddress}.");
            }

            donePassTwo = true;
            return true;
        }

        // Called during pass two (but could be called during pass one!)
        private byte[] AssembleFormat2(Instruction instr)
        {
            if (instr.Format != InstructionFormat.Format2)
                throw new ArgumentException("Instruction must be format 2 to be processed by this method.");

            byte[] ret = new byte[2];
            ret[0] = (byte)instr.Operation;
            ret[1] = (byte)(instr.Operands[0].Value << 4);
            if (instr.Operands.Count > 1)
                ret[1] |= (byte)(instr.Operands[1].Value);

            return ret;
        }

        // Called during pass two.
        private byte[] AssembleFormats34(Instruction instr, int programCounter, int baseRegister)
        {
            int oplen = (int)instr.Format;

            if (oplen != 3 && oplen != 4)
                throw new ArgumentException("Instruction must be format 3 or 4 to be processed by this method.");

            var binInstr = new byte[oplen];
            Array.Clear(binInstr, 0, oplen); // Initialize array to all zeroes. (Not sure if this is necessary.)
            binInstr[0] = (byte)instr.Operation; // Set first byte of instruction to opcode.

            int opcount = instr.Operands.Count;
            if (opcount == 1)
            {
                var firstOperand = instr.Operands[0];

                // If it's a symbol, ensure we include it in the symbol table.
                var sym = firstOperand.SymbolName;
                if (sym != null)
                {
                    sym = TrimIndexer(sym);
                    TouchSymbol(sym);
                }


                // Set flags that don't require knowledge of the displacement, N I X.
                AddressingMode mode = firstOperand.AddressingMode;
                bool indirect = mode.HasFlag(AddressingMode.Indirect);
                bool immediate = mode.HasFlag(AddressingMode.Immediate);
                bool indexed = mode.HasFlag(AddressingMode.Indexed);
                if (indirect)
                {
                    binInstr[0] |= 2; // Set N flag.
                }
                if (immediate)
                {
                    binInstr[0] |= 1; // Set I flag.
                }
                if (indexed)
                {
                    binInstr[1] |= 0x80; // Set X flag.
                }

                // Use extended addressing, if it is indicated.
                int disp = firstOperand.Value.Value;
                if (mode.HasFlag(AddressingMode.Extended))
                {
                    // Use the absolute address as the displacement, and set the E flag.
                    if (immediate)
                    {
                        if (disp < 0)
                            throw new ArgumentException("Displacement cannot be negative using immediate addressing!");
                        const int MAX_F4_DISP = 1 << 20; // untested.
                        if (disp > MAX_F4_DISP)
                            throw new ArgumentException($"Displacement is too large: maximum is {MAX_F4_DISP}.");
                    }
                    // ni xbpe
                    // 21 8421
                    binInstr[1] |= 0x10; // Set E flag.
                    InsertDisplacement(binInstr, disp);
                    return binInstr;
                }

                // Try using program-counter relative addressing.
                const int MIN_PC_DISP = -(1 << 11); // untested.
                const int MAX_PC_DISP = 1 << 11;
                disp = programCounter - disp; // disp now represents the offset between the operand's value and the program counter.
                if (disp >= MIN_PC_DISP && disp <= MAX_PC_DISP)
                {
                    // PC-relative addressing is valid.
                    binInstr[1] |= 0x20; // Set P flag.
                    InsertDisplacement(binInstr, disp);
                    return binInstr;
                }

                // PC-relative addressing failed. Try base-relative addressing.
                // Base-relative addressing will work at execution time only if the value of the base register matches the 'baseRegister' parameter of this method.
                disp = firstOperand.Value.Value - baseRegister;
                const int MIN_BASE_DISP = 0;
                const int MAX_BASE_DISP = 1 << 12; // untested.
                if (disp >= MIN_BASE_DISP && disp <= MAX_BASE_DISP)
                {
                    // Base-relative addressing is valid.
                    binInstr[1] |= 0x40; // Set B flag.
                    InsertDisplacement(binInstr, disp);
                    return binInstr;
                }

                throw new ArgumentException($"Could not assemble format 3 instruction using displacement 0x{firstOperand.Value.Value.ToString("X")}.");

            }

            if (opcount == 0)
            {
                // The only nullary format 3/4 instruction is RSUB.
                if (instr.Operation != Instruction.Mnemonic.RSUB)
                    throw new ArgumentException($"Missing operand(s) for instruction {instr.ToString()}.");

                // What should we expect addressing mode to be here?
                // all flags zero?
                // ni=11, rest zero?
            }
            throw new ArgumentException($"Too many operands for format {oplen} instruction {instr.ToString()}.");
        }

        private static void InsertDisplacement(byte[] instruction, int displacement) // untested
        {
            int len = instruction.Length;
            byte[] dispBytes;
            switch (len)
            {
                case 3:
                    dispBytes = EncodeTwosComplement(displacement, 12);
                    Debug.Assert(dispBytes.Length == 2, "encodetwoscomplement gave us wrong number of bytes!");
                    Debug.Assert((instruction[1] & 0b000011) == 0, "disp bits are already set in instruction!");
                    Debug.Assert(instruction[2] == 0, "disp bits are already set in instruction!");
                    instruction[2] = dispBytes[0];
                    instruction[1] |= dispBytes[1];
                    break;
                case 4:
                    dispBytes = EncodeTwosComplement(displacement, 20);
                    Debug.Assert(dispBytes.Length == 3, "encodetwoscomplement gave us wrong number of bytes!");
                    Debug.Assert((instruction[1] & 0b000011) == 0, "disp bits are already set in instruction!");
                    Debug.Assert(instruction[2] == 0, "disp bits are already set in instruction!");
                    Debug.Assert(instruction[3] == 0, "disp bits are already set in instruction!");
                    instruction[3] = dispBytes[0];
                    instruction[2] = dispBytes[1];
                    instruction[1] |= dispBytes[2];
                    break;
                default:
                    throw new ArgumentException("Instruction length must be 3 or 4 bytes in length.");
            }
        }

        /// <summary>
        /// Removes ",x" from the end of the string, if it is present.
        /// </summary>
        private static string TrimIndexer(string symbol)
        {
            const string INDEX_SUFFIX = ",x";
            if (symbol.EndsWith(INDEX_SUFFIX))
            {
                return symbol.Substring(0, symbol.Length - INDEX_SUFFIX.Length);
            }
            return symbol;
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
                if (existing.Address.HasValue)
                {
                    Console.WriteLine($"Symbol \"{name}\" already has value {existing.Address}!");
                    return false;
                }
                existing.Address = value;
            }
            symbols.Add(name, new Symbol(name) { Address = value });
            return true;
        }

        private static byte[] EncodeTwosComplement(int n, int bits) // untested.
        {
            int highMask = checked(~((1 << bits) - 1));
            if ((highMask & bits) > 0)
            {
                // Higher bits are set in 'n' than 2^bits.
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

            int retlen = (int)Math.Ceiling(bits / 8d);
            var ret = new byte[retlen];
            for (int b = 0; b < retlen; ++b)
            {
                int only = (0xff << (b * 8)) & n;
                ret[b] = (byte)(only >> (b * 8));
            }
            return ret;

            //if (high == 0)
            //{
            //    if (middle == 0)
            //    {
            //        return new byte[] { low };
            //    }
            //    return new byte[] { low, middle };
            //}
            //return new byte[] { low, middle, high };

        }
    }
}
