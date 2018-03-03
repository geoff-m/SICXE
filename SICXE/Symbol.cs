using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXE
{
    /// <summary>
    /// A placeholder for a symbol specified in a program, to which the assembler will assign an address.
    /// </summary>
    class Symbol
    {
        static int _id = 0;
        public int ID
        { get; private set; }
        public string Name
        { get; private set; }
        public Symbol(string name)
        {
            ID = _id++;
            Name = name;
        }

        public int? Value
        { get; set; }

        public override string ToString()
        {
            if (Value.HasValue)
            {
                return $"{Name}={Value}";
            }else
            {
                return $"{Name}=<not set>";
            }
            
        }
    }
}
