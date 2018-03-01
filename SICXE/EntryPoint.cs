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
            var myProgram = Program.Parse(@"E:\Google Drive\Intro to System Software\asms\small.txt");

            //var myProgram = Program.Parse(@"C:\Users\geoff\Google Drive\Intro to System Software\asms\copy-tix-add.asm");

            for (int i = 0; i < myProgram.Count; ++i)
            {
                Console.WriteLine($"{myProgram[i].ToString()}");
            }

            //var myMachine = new vsic.Machine();

            var myBinary = Assembler.Assemble(myProgram);


        }
    }
}
