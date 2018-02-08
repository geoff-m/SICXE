﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICXE
{
    class AssemblerDirective : Line
    {
        public enum Mnemonic
        {
            EQU = 1,
            RESW = 2,
            BYTE = 3
            // etc.
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
            if (tokens.Length == 0 || tokens.Length > 2)
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
                case Mnemonic.BYTE:
                case Mnemonic.RESW:
                case Mnemonic.EQU:
                    result.Value = tokens[1];
                    break;
                    // todo: parse arguments properly for each directive.
            }
            return true;
        }

        public override string ToString()
        {
            if (Label != null)
            {
                return $"{Label}: {Directive.ToString()} {Value}";
            }
            return $"{Label}: {Directive.ToString()} {Value}";
        }
    }
}