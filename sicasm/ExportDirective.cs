using System;
using System.Collections.Generic;
using System.Linq;

namespace SICXEAssembler
{
    // This isn't based on the book. I just made it up. See text file.
    internal class ExportDirective : Line
    {
        public static bool TryParse(string[] tokens, out ExportDirective ed)
        {
            if (tokens.Length < 1 || tokens[0].Length == 0)
            {
                ed = null;
                return false;
            }
            if (tokens[0] == "@export")
            {
                if (tokens.Length < 2)
                {
                    Console.Error.WriteLine("Expected symbol name after @export");
                    ed = null;
                    return false;
                }
                if (tokens.Length > 2)
                {
                    Console.Error.WriteLine("Too many tokens after @export");
                    ed = null;
                    return false;
                }

                var ret = new ExportDirective();
                ret.Label = tokens[1];
                ed = ret;
                return true;
            }
            ed = null;
            return false;
        }

        public override string ToString(int space)
        {
            string addrStr = "";
            if (Address.HasValue)
            {
                addrStr = $" ({Address.Value.ToString("X6")})";
            }
            return $"@export {Label}{addrStr}";
        }
    }
}
