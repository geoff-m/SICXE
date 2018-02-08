using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICXE
{
    class Assembler
    {
        public static Word[] Assemble(Program prog)
        {
            var ret = new List<Word>();

            foreach (AssemblerDirective dir in prog.Where(l => l is AssemblerDirective))
            {

            }

            return null;
        }
    }
}
