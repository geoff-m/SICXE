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

        public string Comment
        { get; set; }

        /// <summary>
        /// The address a line has in the assembled binary.
        /// </summary>
        public int? Address
        { get; set; }

        /// <summary>
        /// The number this line had in the original file.
        /// </summary>
        public int Number
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

            /*  Update: A (non-comment) input line should be interpreted as having a label iff its first character is not whitespace.
             *  Therefore we'll use this condition to determine if the first token should be interpreted as a label.
             *  However, I do not like this rule and think the user should be able to give us a line withouth a label that nevertheless begins without any whitespace.
             *  So, I have resolved to assume the rule is followed, but if parsing in that way fails, I will try assuming the rule is not followed.
             *  If the latter succeeds, I will proceed while emitting a warning.
             */

            if (char.IsWhiteSpace(s[0]))
            {
                if (TryParseWithoutLabel(tokens, out result))
                {
                    // The line had no label.
                    return true;
                }
                else
                {
                    // Treat the first token as a label.
                    if (TryParseWithoutLabel(tokens.Skip(1).ToArray(), out result))
                    {
                        Console.WriteLine($"Warning: Extra whitespace at start of line \"{s}\"");
                        result.Label = tokens[0];
                        return true;
                    }
                }
            }
            else
            {
                // Treat the first token as a label.
                if (TryParseWithoutLabel(tokens.Skip(1).ToArray(), out result))
                {
                    result.Label = tokens[0];
                    return true;
                }
                else
                {
                    if (TryParseWithoutLabel(tokens, out result))
                    {
                        // The line had no label.
                        if (result is AssemblerDirective)
                        {
                            Console.WriteLine($"Warning: Assembler directive should be preceded by whitespace: \"{s}\"");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Instruction should be preceded by whitespace: \"{s}\"");
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryParseWithoutLabel(string[] tokens, out Line result)
        {
            // Attempt to parse as instruction.
            Instruction inst;
            if (Instruction.TryParse(tokens, out inst))
            {
                result = inst;
                return true;
            }

            // Attempt to parse as directive.
            AssemblerDirective dir;
            if (AssemblerDirective.TryParse(tokens, out dir))
            {
                result = dir;
                return true;
            }

            result = null;
            return false;
        }

        //        public abstract string Verbatim;

        /// <summary>
        /// Converts this line to a string representation, using the specified amount of whitespace after the label, if it is present.
        /// </summary>
        /// <param name="space">The number of spaces to insert after the label.</param>
        /// <returns></returns>
        public abstract string ToString(int space);
    }
}
