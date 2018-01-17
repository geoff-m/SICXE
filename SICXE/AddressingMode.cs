using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXE
{
    enum AddressingMode
    {
        NotSet = 0,
        Direct = 1,
        RelativeToPC = 2,
        RelativeToBase = 3,
        Immediate = 4,
        Indirect = 5
    }
}
