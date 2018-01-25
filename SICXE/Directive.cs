using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXE
{
    class Directive : Line
    {
        public enum Mnemonic
        {
            EQU = 1,
            RESW = 2,
            BYTE = 3
            // etc.
        }

        public Mnemonic Keyword
        { get; private set; }

        public Directive(Mnemonic d)
        {
            Keyword = d;
        }

        public string Value // ("operand")
        { get; set; }

        /// <summary>
        /// Parses a string containing assembler directive.
        /// </summary>
        /// <param name="s">An assembler directive, possibly including arguments.</param>
        /// <param name="result">If the parse is successful, the Directive represented by the given string. Otherwise, null.</param>
        /// <returns>A Boolean value indicating whether the parse was successful.</returns>
        public static bool TryParse(string s, out Directive result)
        {
            
        }
    }
}
