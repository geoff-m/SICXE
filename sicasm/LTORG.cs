using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXEAssembler
{
    class LTORG : AssemblerDirective
    {
        public LTORG() : base(Mnemonic.LTORG)
        {
            Literals = new List<Literal>();
        }

        public IList<Literal> Literals
        { get; private set; }
    }
}
