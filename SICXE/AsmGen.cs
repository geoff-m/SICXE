using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using EnumsNET;

namespace SICXE
{
    /// <summary>
    /// Generates assembly programs for the purpose of testing the assembler.
    /// </summary>
    class AsmGen // todo: rework this class so that it is stateful. this will greatly improve modularity and ease of writing
    {
        static Random _r = new Random();
        static readonly Instruction.Mnemonic[] _MNEMONICS = (Instruction.Mnemonic[])Enum.GetValues(typeof(Instruction.Mnemonic));
        static readonly Register[] _REGISTERS = (Register[])Enum.GetValues(typeof(Register));

        public static Program MakeRandomProgram(int lines)
        {
            const double CHANCE_MEMORY = 0.2; // Chance a line will be a memory-reserving directive instead of an instruction.

            const double CHANCE_RESW = 0.5; // Chance a region of memory will be RESW instead of WORD.

            const double CHANCE_USE_SYMBOL = 0.75; // Chance a symbol will be created/referenced where possible.

            const double CHANCE_NEW_SYMBOL = 0.25; // Chance a referenced symbol will be a newly created one instead of a preexisting one.

            const double CHANCE_INDIRECT = 0.25; // Chance indirect addressing will be used.
            const double CHANCE_IMMEDIATE = 0.25; // Chance immediate addressing will be used.
            const double CHANCE_EXTENDED = 0.25; // Chance extended addressing will be used.            

            var ret = new Program();

            var symbols = new HashSet<string>();

            for (int lineIdx = 0; lineIdx < lines; ++lineIdx)
            {
                Line line;
                // Make a WORD or RESW.
                if (_r.NextDouble() < CHANCE_MEMORY)
                {
                    AssemblerDirective dir;
                    if (_r.NextDouble() < CHANCE_RESW)
                    {
                        dir = new AssemblerDirective(AssemblerDirective.Mnemonic.WORD);
                    }
                    else
                    {
                        dir = new AssemblerDirective(AssemblerDirective.Mnemonic.RESW);
                    }
                    if (_r.NextDouble() < CHANCE_USE_SYMBOL)
                    {
                        string symbol;
                        if (symbols.Count == 0 || _r.NextDouble() < CHANCE_NEW_SYMBOL)
                        {
                            // Create a new symbol.
                            bool addOK;
                            do
                            {
                                symbol = GetRandomSymbol();
                                addOK = symbols.Add(symbol);
                            } while (!addOK);
                        }
                        else
                        {
                            // Pick a random existing symbol.
                            symbol = symbols.ElementAt(_r.Next(symbols.Count));
                        }
                        dir.Label = symbol;
                    }
                }
                else
                {
                    // Make an instruction.
                    //Instruction.Mnemonic.
                    Instruction instr = new Instruction(_MNEMONICS[_r.Next(_MNEMONICS.Length)]);

                    for (int operandIdx = 0; operandIdx < instr.Operands.Count; ++operandIdx)
                    {
                        var operand = instr.Operands[operandIdx];
                        switch (instr.Format)
                        {
                            case InstructionFormat.Format2: // If format 2, choose register.
                                operand.Value = (int)_REGISTERS[_r.Next(_REGISTERS.Length)];
                                break;
                            case InstructionFormat.Format3:
                            case InstructionFormat.Format4: // If format 3/4, choose whether a symbol will be referenced.
                                if (_r.NextDouble() < CHANCE_USE_SYMBOL)
                                {
                                    string symbol;
                                    // Choose whether existing symbol will be used or if new one will be created.
                                    if (_r.NextDouble() < CHANCE_NEW_SYMBOL)
                                    {
                                        // Create a new symbol.
                                        bool addOK;
                                        do
                                        {
                                            symbol = GetRandomSymbol();
                                            addOK = symbols.Add(symbol);
                                        } while (!addOK);
                                    }
                                    else
                                    {
                                        // Choose a random existing symbol.
                                        symbol = symbols.ElementAt(_r.Next(symbols.Count));
                                    }
                                    operand.SymbolName = symbol;
                                }
                                // Choose direct/indirect.
                                if (_r.NextDouble() < CHANCE_INDIRECT)
                                    operand.AddressingMode = AddressingMode.Indirect;
                                // Choose immediate.
                                if (_r.NextDouble() < CHANCE_IMMEDIATE)
                                    operand.AddressingMode |= AddressingMode.Immediate;
                                // Choose extended.
                                if (_r.NextDouble() < CHANCE_EXTENDED)
                                    operand.AddressingMode |= AddressingMode.Extended;
                                break;
                        }
                    } // For each operand.
                } // Choose between making this line a WORD or an instruction.
            } // For each line in output program.
        }

        /// <summary>
        /// Generates a random name for a symbol.
        /// </summary>
        /// <returns></returns>
        private static string GetRandomSymbol()
        {
            const int MINIMUM_SYMBOL_LENGTH = 2; // inclusive.
            const int MAXIMUM_SYMBOL_LENGTH = 7; // exclusive.

            char c = (char)(_r.Next(0, 26) + 'a');
            return new string(c, _r.Next(MINIMUM_SYMBOL_LENGTH, MAXIMUM_SYMBOL_LENGTH));
        }
    }
}
