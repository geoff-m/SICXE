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
            var myprog = new Program();
            myprog.Add(new Instruction(Operation.LDA, 1337, AddressingMode.Immediate)); // A <-- 1337
            myprog.Add(new Instruction(Operation.STA, 0, AddressingMode.Immediate)); // mem[0] <-- A
            myprog.Add(new Instruction(Operation.ADD, 3, AddressingMode.Immediate)); // A += 3
            myprog.Add(new Instruction(Operation.STA, 5, AddressingMode.Immediate)); // mem[5] <-- A

            var mymachine = new Machine();

            mymachine.Execute(myprog);

            mymachine.DumpWords(0, 20);

        }
    }
}
