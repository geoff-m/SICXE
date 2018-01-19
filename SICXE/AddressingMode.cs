using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXE
{
    enum AddressingMode
    {
        NotSet = 0, // Should not be used--indicates a problem.

        // These are the three that can be specified in a program.
        Simple = 1,
        Immediate = 2,
        Indirect = 3,


        // These do not exist as far as a program is concerned, but are of concern to assemblers and machines.
        RelativeToProgramCounter = 8,
        RelativeToBase = 9
            // RelativeToIndex...?
    }
}
