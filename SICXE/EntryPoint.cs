using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXE
{
    class EntryPoint
    {
        static void Main(string[] args)
        {
            var myProgram = Program.Parse("prog1.txt");

            var myMachine = new sicsim.Machine();

            var myBinary = Assembler.Assemble(myProgram);


            

        }
    }
}
