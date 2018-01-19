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
            var parser = new ProgramParser();
            var prog = parser.ParseProgram("prog1.txt");

            var mymachine = new Machine();

            mymachine.Execute(prog);

            mymachine.DumpWords(0, 20);

        }
    }
}
