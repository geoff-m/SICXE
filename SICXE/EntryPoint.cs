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

            var myMachine = new Machine();

            myMachine.Execute(myProgram);

            myMachine.PrintWords(0, 20);

            var myBinary = Assembler.Assemble(myProgram);
            myMachine.DMAWrite(myBinary, 0); // Copy the program onto the machine at address 0.
            myMachine.Run(0); // Run the machine from address 0.

            

        }
    }
}
