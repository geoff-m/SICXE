using System;
using System.Collections.Generic;
using System.Linq;

namespace SICXEAssembler
{
    internal struct ExportedSymbol
    {
        /// <summary>
        /// The base address of the module that contains this symbol.
        /// </summary>
        public readonly int ModuleBaseAddress;
        public readonly Program OriginProgram;
        public readonly Symbol Symbol;
        public ExportedSymbol(Program originProgram, int moduleBaseAddress, Symbol symbol)
        {
            OriginProgram = originProgram;
            ModuleBaseAddress = moduleBaseAddress;
            Symbol = symbol;
        }

        public override string ToString()
        {
            if (Symbol.Address.HasValue)
                return $"\"{Symbol}\" (offset {Symbol.Address.Value} in {OriginProgram})";
            else
                return $"\"{Symbol}\" (defined in {OriginProgram} (unknown offset))";

        }

        public static void PrintTable(IList<ExportedSymbol> list)
        {
            if (list.Count == 0)
            {
                Console.WriteLine("--No exported symbols--");
                return;
            }
            var maxNameLength = list.Max(es => es.Symbol.Name.Length);
            if (maxNameLength < 4)
                maxNameLength = 4;
            Console.WriteLine($"{"Name".PadRight(maxNameLength)}  Absolute Address  File");
            foreach (var es in list)
            {
                var sym = es.Symbol;
                var addrString = sym.Address.HasValue ? (sym.Address.Value + es.ModuleBaseAddress).ToString("X6") : "??????";
                Console.WriteLine($"{sym.Name.PadRight(maxNameLength)}  0x{addrString}          {es.OriginProgram.OriginFile}");
            }
        }
    }
}
