using System;
using System.Collections.Generic;
using System.Linq;

namespace SICXE
{
    class AssemblerDirective : Line
    {
        public enum Mnemonic
        {
            EQU = 1,
            RESW = 2,
            BYTE = 3,
            WORD = 4,

            START = 10,
            END = 11,
            BASE = 12, // todo: implement me
            CSECT = 13, // not yet implemented
            
            LTORG = 20
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
            if (!Enum.TryParse(tokens[0], true, out Mnemonic dir))
            {
                result = null;
                return false;
            }

            result = new AssemblerDirective(dir);
            switch (dir)
            {
                // todo: parse arguments properly for each directive.
                case Mnemonic.BYTE:
                case Mnemonic.RESW:
                case Mnemonic.EQU:
                case Mnemonic.START:
                case Mnemonic.WORD:
                    if (tokens.Length < 2) // Argument of these is required.
                    {
                        result = null;
                        return false;
                    }
                    result.Value = tokens[1];
                    result.Comment = string.Join(" ", tokens, 2, tokens.Length - 2);
                    break;
                case Mnemonic.END:
                    if (tokens.Length > 1) // Argument of END is optional.
                    {
                        result.Value = tokens[1];
                        result.Comment = string.Join(" ", tokens, 2, tokens.Length - 2);
                    }
                    break;
                case Mnemonic.LTORG:
                    // LTORG takes no arguments.
                    result.Comment = string.Join(" ", tokens, 1, tokens.Length - 1);
                    break;

                default:
                    throw new NotImplementedException($"Assembler directive \"{tokens[0]}\" is not supported!");
            }

            

            return true;
        }

        public override string ToString()
        {
#if DEBUG
            if (Label != null)
                return $"{Label}: {Directive.ToString()} {Value}";
            return $"{Directive.ToString()} {Value}";
#else
            if (Label != null)
                return $"{Label}\t{Directive.ToString()} {Value}";
            return $"\t\t{Directive.ToString()} {Value}";
#endif
        }

        public override string ToString(int space)
        {
            if (Label != null)
                return $"{Label}{new string(' ', space - Label.Length + 2)}{Directive.ToString()} {Value}";
            return $"{new string(' ', space + 2)}{Directive.ToString()} {Value}";
        }
    }
}
