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
            PassOne();


            result = null;
            return false;
        }

        Program prog;
        public Assembler(Program p)
        {
            prog = p;
        }

        Dictionary<string, Symbol> symbols;
        bool donePassOne = false;
        private bool PassOne()
        {
            if (donePassOne)
            {
                throw new InvalidOperationException("Pass one has already been done!");
            }

            symbols = new Dictionary<string, Symbol>();
            foreach (var line in prog)
            {
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
                        case AssemblerDirective.Mnemonic.WORD:
                        case AssemblerDirective.Mnemonic.RESW:
                            if (int.TryParse(dir.Value, out val))
                            {
                                if (line.Label != null)
                                    if (!SetSymbolValue(line.Label, val))
                                    return false;
                            }
                            else
                            {
                                // todo: some word/byte directives don't have the form of an integer. handle these.
                                Console.WriteLine($"Could not parse integer \"{dir.Value}\"");
                                return false;
                            }
                            break;
                        // We allow this directive to have either an integer or a symbol as its argument.
                        case AssemblerDirective.Mnemonic.START:
                        case AssemblerDirective.Mnemonic.END:
                            if (dir.Value == null)
                            {
                                Console.WriteLine("START and END directives must have a value!");
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
                                UseSymbol(dir.Value);
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
                    var operands = ((Instruction)line).Operands;
                    if (operands.Count == 1)
                    {
                        var operandSymbol = operands[0].Symbol;
                        if (operandSymbol != null)
                            UseSymbol(operandSymbol);
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
        private void UseSymbol(string name)
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
    }
}
