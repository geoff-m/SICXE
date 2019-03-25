using System;
using System.Collections.Generic;
using System.Linq;

namespace SICXEAssembler
{
    public class AssemblerDirective : Line
    {
        public enum Mnemonic
        {
            RESB = 1, // See p. 44
            RESW = 2, // See p. 44
            BYTE = 3, // See p. 44
            WORD = 4, // See p. 44

            START = 10, // See p. 44
            END = 11, // See p. 44
            BASE = 12,
            CSECT = 13, // not yet implemented
            
            LTORG = 20,
                EQU = 30
        }

        public Mnemonic Directive
        { get; private set; }

        public AssemblerDirective(Mnemonic d)
        {
            Directive = d;
        }

        public string Value // ("operand")
        { get; set; }

        /// <summary>
        /// Parses a string containing assembler directive.
        /// </summary>
        /// <param name="s">An array beginning with a an assembler directive, possibly including arguments.</param>
        /// <param name="result">If the parse is successful, the Directive represented by the given string. Otherwise, null.</param>
        /// <returns>A Boolean value indicating whether the parse was successful.</returns>
        public static bool TryParse(string[] tokens, out AssemblerDirective result)
        {
            if (tokens.Length < 1)
            {
                result = null;
                return false;
            }
            Mnemonic dir;
            if (!Enum.TryParse(tokens[0], true, out dir))
            {
                result = null;
                return false;
            }

            switch (dir)
            {
                // parse arguments properly for each directive.
                case Mnemonic.BYTE:
                case Mnemonic.RESW:
                case Mnemonic.RESB:
                case Mnemonic.EQU:
                case Mnemonic.START:
                case Mnemonic.WORD:
                case Mnemonic.BASE:
                    if (tokens.Length < 2) // These directives require an argument.
                    {
                        result = null;
                        return false;
                    }
                    result = new AssemblerDirective(dir);
                    result.Value = tokens[1];
                    result.Comment = string.Join(" ", tokens, 2, tokens.Length - 2);
                    break;
                case Mnemonic.END:
                    result = new AssemblerDirective(dir);
                    if (tokens.Length > 1) // Argument of END is optional.
                    {
                        result.Value = tokens[1];
                        result.Comment = string.Join(" ", tokens, 2, tokens.Length - 2);
                    }
                    break;
                case Mnemonic.LTORG:
                    // LTORG takes no arguments.
                    result = new LTORG();
                    result.Comment = string.Join(" ", tokens, 1, tokens.Length - 1);
                    break;

                default:
                    throw new NotImplementedException($"Assembler directive \"{tokens[0]}\" is not supported!");
            }

            return true;
        }

        public override string ToString()
        {
            if (Label != null)
                return $"{Label}\t{Directive.ToString()} {Value} {Comment}";
            return $"{Directive.ToString()} {Value} {Comment}";
        }

        public override string ToString(int space)
        {
            if (Label != null)
                return $"{Label}{new string(' ', Math.Max(1, space - Label.Length + 2))}{Directive.ToString()} {Value} {Comment}";
            return $"{new string(' ', space + 2)}{Directive.ToString()} {Value} {Comment}";
        }
    }
}
