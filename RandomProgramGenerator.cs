using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
//using EnumsNET;
using System.Timers;
using Interlocked = System.Threading.Interlocked;

namespace SICXE
{
    /// <summary>
    /// Generates assembly programs for the purpose of testing the assembler.
    /// </summary>
    class RandomProgramGenerator // todo: rework this class so that it is stateful. this will greatly improve modularity and ease of writing
    {
        const double CHANCE_MEMORY = 0.2; // Chance a line will be a memory-reserving directive instead of an instruction.
        const double CHANCE_RESW = 0.5; // Chance a region of memory will be RESW instead of WORD.
        const double CHANCE_USE_SYMBOL = 0.75; // Chance a symbol will be created/referenced where possible.
        const double CHANCE_NEW_SYMBOL = 0.25; // Chance a referenced symbol will be a newly created one instead of a preexisting one.
        const double CHANCE_INDIRECT = 0.25; // Chance indirect addressing will be used.
        const double CHANCE_IMMEDIATE = 0.25; // Chance immediate addressing will be used.
        const double CHANCE_INDEXED = 0.2; // Chance indexed addressing will be used. // todo: implement me
        const double CHANCE_EXTENDED = 0.25; // Chance extended addressing will be used.

        const int MIN_WORD = -0xffffff;
        const int MAX_WORD = 0x7fffff;

        readonly Instruction.Mnemonic[] MNEMONICS;
        readonly Register[] REGISTERS;
        Random r;
        public RandomProgramGenerator()
        {
            r = new Random();
            MNEMONICS = (Instruction.Mnemonic[])Enum.GetValues(typeof(Instruction.Mnemonic));
            REGISTERS = new Register[] { Register.A, Register.B, Register.L, Register.S, Register.T, Register.X };

            symbols = new List<Symbol>(); // This is a private Symbol class (defined in this file), not the one used by the assembler.
        }


        /// <summary>
        /// Generates a random number of symbols (expected value = lineCount * CHANCE_MEMORY * CHANCE_USE_SYMBOL * CHANCE_NEW_SYMBOL).
        /// </summary>
        /// <param name="lineCount">The approximate total number of lines in the program.</param>
        private void GenerateSymbols(int lineCount)
        {
            const double THRESHOLD = CHANCE_MEMORY * CHANCE_USE_SYMBOL;
            Console.WriteLine($"Generating about {(int)(lineCount * THRESHOLD)} symbols...");
            var names = new HashSet<string>();
            for (int i = 0; i < lineCount; ++i)
            {
                if (r.NextDouble() < THRESHOLD)
                {
                    bool addOk = false;
                    do
                    {
                        addOk = names.Add(CreateRandomSymbolName());
                    } while (!addOk);
                }
            }
            foreach (var name in names)
                symbols.Add(new Symbol(name));
            Console.WriteLine($"Generated {symbols.Count} symbols.");
        }

        System.Timers.Timer t;

        private void TimerElapsed(object sender, EventArgs e)
        {
            long dLines = Interlocked.Read(ref linesProcessed);
            Interlocked.Exchange(ref linesProcessed, 0);
            double dTime = t.Interval / 1000d; // s
            OutputSameLine($"Rate: {(int)(dLines / dTime)} lines/sec");
        }

        private void OutputSameLine(string str)
        {
            int top = Console.CursorTop;
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.WindowWidth));
            Console.CursorLeft = 0;
            Console.CursorTop = top;
            Console.Write(str);
            Console.CursorTop = top;
        }

        long linesProcessed = 0;

        public Program MakeRandomProgram(int lines)
        {
            t = new Timer(3000);
            t.Elapsed += TimerElapsed;
            symbols.Clear();
            GenerateSymbols(lines);
            var ret = new Program();
            t.Start();

            ret.Add(new AssemblerDirective(AssemblerDirective.Mnemonic.START)
            {
                //Value = r.Next(0, 1 << 12).ToString()
                Value = "0"
            });

            for (int lineIdx = 0; lineIdx < lines; ++lineIdx)
            {
                Line line;
                // Make a WORD or RESW.
                if (r.NextDouble() < CHANCE_MEMORY)
                {
                    AssemblerDirective dir;
                    if (r.NextDouble() < CHANCE_RESW)
                    {
                        dir = new AssemblerDirective(AssemblerDirective.Mnemonic.WORD);
                    }
                    else
                    {
                        dir = new AssemblerDirective(AssemblerDirective.Mnemonic.RESW);
                    }
                    if (r.NextDouble() < CHANCE_USE_SYMBOL)
                    {
                        // The symbol we put here must not have been defined elsewhere.
                        var sym = GetUndefinedSymbol();
                        dir.Label = sym.Name;
                        sym.Define();
                    }
                    dir.Value = GetSmallishNumber().ToString();
                    line = dir;
                }
                else
                {
                    // Make an instruction.
                    //Instruction.Mnemonic.
                    Instruction instr = new Instruction(MNEMONICS[r.Next(MNEMONICS.Length)]);

                    for (int operandIdx = 0; operandIdx < instr.Operands.Count; ++operandIdx)
                    {
                        var operand = instr.Operands[operandIdx];
                        switch (instr.Format)
                        {
                            case InstructionFormat.Format2:
                                // Choose register.
                                operand.Value = (int)REGISTERS[r.Next(REGISTERS.Length)];
                                break;
                            case InstructionFormat.Format3:
                            case InstructionFormat.Format4:
                            case InstructionFormat.Format3Or4:
                                // Choose whether a symbol will be referenced.
                                if (r.NextDouble() < CHANCE_USE_SYMBOL)
                                {
                                    operand.SymbolName = GetSymbol();
                                }
                                else
                                {
                                    // Use immediate instead of symbol.
                                    operand.Value = r.Next(-0x7ff, 0x800);
                                }
                                // Choose direct/indirect.
                                if (r.NextDouble() < CHANCE_INDIRECT)
                                    operand.AddressingMode = AddressingMode.Indirect;
                                // Choose immediate.
                                if (r.NextDouble() < CHANCE_IMMEDIATE)
                                    operand.AddressingMode |= AddressingMode.Immediate;
                                // Choose extended.
                                if (r.NextDouble() < CHANCE_EXTENDED)
                                {
                                    //operand.AddressingMode |= AddressingMode.Extended;
                                    instr.Format = InstructionFormat.Format4;
                                }
                                break;
                        }
                    } // For each operand.

                    line = instr;
                } // Choose between making this line a WORD or an instruction.

                ret.Add(line);
                Interlocked.Increment(ref linesProcessed);
            } // For each line in output program.

            // Finally, append any symbols that are still undefined by this point.
            foreach (var s in symbols)
            {
                if (!s.IsDefined)
                {
                    AssemblerDirective dir;
                    if (r.NextDouble() < CHANCE_RESW)
                        dir = new AssemblerDirective(AssemblerDirective.Mnemonic.WORD);
                    else
                        dir = new AssemblerDirective(AssemblerDirective.Mnemonic.RESW);
                    dir.Label = s.Name;
                    s.Define();
                    dir.Value = GetSmallishNumber().ToString();
                    ret.Add(dir);
                }
            }

            t.Stop();

            Console.WriteLine();
            return ret;
        }

        const int MINIMUM_SYMBOL_LENGTH = 2; // inclusive.
        const int MAXIMUM_SYMBOL_LENGTH = 7; // exclusive.

        List<Symbol> symbols;

        /// <summary>
        /// Gets a symbol name for use in a program.
        /// </summary>
        private string GetSymbol()
        {
            return symbols[r.Next(symbols.Count)].Name;
        }

        private int GetSmallishNumber()
        {
            int b = r.Next(sizeof(int));
            return r.Next() & ((1 << b) - 1);
        }

        /// <summary>
        /// Gets a symbol for use in a program, but necessarily one that hasn't been defined already (by WORD or RESW).
        /// </summary>
        private Symbol GetUndefinedSymbol()
        {
            var undefs = symbols.Where(s => !s.IsDefined).ToList();
            if (!undefs.Any())
            {
                // Create a new symbol because we are out of undefined ones.
                string newName;
                do
                {
                    newName = CreateRandomSymbolName();
                } while (symbols.Any(s => s.Name == newName));
                var ret = new Symbol(newName);
                symbols.Add(ret);
                return ret;
            }
            // Choose a random existing symbol.
            return undefs.ElementAt(r.Next(undefs.Count));
        }

        private string CreateRandomSymbolName()
        {
            int len = r.Next(MINIMUM_SYMBOL_LENGTH, MAXIMUM_SYMBOL_LENGTH);
            var ret = new char[len];
            for (int i = 0; i < len; ++i)
            {
                ret[i] = (char)(r.Next(0, 26) + 'a');
            }
            return new string(ret);
        }

        private class Symbol
        {
            public string Name
            { get; set; }
            public bool IsDefined
            { get; private set; }
            public Symbol(string name)
            {
                Name = name;
                IsDefined = false;
            }
            public void Define()
            {
                if (IsDefined)
                    throw new InvalidOperationException("This symbol has already been marked as defined!");
                IsDefined = true;
            }
        }
    }
}
