using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace sicdisasm
{
    class Program
    {
        static void Main(string[] args)
        {
            var bstr = "C0C10000FF0000004C0000B400E32FF03320033F200C032FEC2B2FE43B2FE93F2FEBDB2FDB2B2FDA332FDDDF2FD36D00017F2FD13F2FD6ffffffffffffffffffff";
            var data = ByteArrayFromString(bstr);

            const int startAt = 0;
            var dm = new Disassembler();
            var disasm = dm.DisassembleWithContinue(new MemoryStream(data), startAt, data.Length - startAt);
            Console.WriteLine($"Disassembled {disasm.Instructions.Count} instructions.\n");

            const int MAX_BYTES_LENGTH = 14;
            Console.WriteLine($"Address   Bytes          Instructions");
            foreach (var instr in disasm.Instructions)
            {
                int instrLen = (int)instr.Format;
                int instrAddr = instr.Address.Value;
                var instrBytes = string.Join(" ", Enumerable.Range(instrAddr, instrLen).Select(i => data[i].ToString("X2")));
                string addrString = instr.Address.HasValue ? instrAddr.ToString("X6") : "??????";
                Console.WriteLine($"0x{addrString}  {instrBytes.PadRight(MAX_BYTES_LENGTH)} {instr.ToString()}");
            }
            
        }

        static byte[] ByteArrayFromString(string str)
        {
            int len = str.Length;
            Debug.Assert(len % 2 == 0);
            int byteCount = len / 2;
            var ret = new byte[byteCount];
            for (int i = 0; i < byteCount; ++i)
            {
                byte high = NibbleFromChar(str[i * 2]);
                byte low = NibbleFromChar(str[i * 2 + 1]);
                ret[i] = (byte)(high << 4 | low);
            }
            return ret;
        }

        static byte NibbleFromChar(char c)
        {
            if (c >= '0' && c <= '9')
                return (byte)(c - '0');
            c = char.ToLower(c);
            if (c >= 'a' && c <= 'f')
                return (byte)(c - 'a' + 0xa);
            throw new ArgumentException();
        }
    }
}
