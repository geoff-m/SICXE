using System;
using System.Collections.Generic;
using System.Linq;

namespace SICXE
{
    /// <summary>
    /// Represents a line in a user-input assembly file (a program).
    /// </summary>
    abstract class Line
    {
        public string Label
        { get; set; }

        /// <summary>
        /// Parses a line as either a Directive or an Instruction.
        /// </summary>
        /// <param name="s">A line containing either an assembler directive or a SIC/XE instruction.</param>
        /// <param name="result">If the parse was successful, the line represented by the given string. Otherwise, null.</param>
        /// <returns>A Boolean value indicating whether the parse was successful.</returns>
        public static bool TryParse(string s, out Line result)
        {
            var tokens = s.SmartSplit();

            /* We must deal with the question of whether the first token is a label.
            * Try parsing the first token as a Directive.Mnemonic or as an Instruction.Mnemonic.
            * If these both fail, we conclude the first token is a label.
            */

            if (TryParseWithoutLabel(tokens, out result))
            {
                // The line had no label.
                return true;
            }

            // Treat the first token as a label.
            if (TryParseWithoutLabel(tokens.Skip(1).ToArray(), out result))
            {
                result.Label = tokens[0];
                return true;
            }

            return false;
        }


        private static bool TryParseWithoutLabel(string[] tokens, out Line result)
        {
            // Attempt to parse as instruction.
            if (Instruction.TryParse(tokens, out Instruction inst))
            {
                result = inst;
                return true;
            }

            // Attempt to parse as directive.
            if (AssemblerDirective.TryParse(tokens, out AssemblerDirective dir))
            {
                result = dir;
                return true;
            }

            result = null;
            return false;
        }
    }
}
