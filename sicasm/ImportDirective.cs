using System;
using System.Collections.Generic;
using System.Linq;

namespace SICXEAssembler
{
    // This isn't based on the book. I just made it up. See text file.
    internal class ImportDirective : Line
    {
        /// <summary>
        /// The file that contains the correpsonding export, if known.
        /// </summary>
        public string ExporterFile;

        public static bool TryParse(string[] tokens, out ImportDirective id)
        {
            if (tokens.Length < 1 || tokens[0].Length == 0)
            {
                id = null;
                return false;
            }
            if (tokens[0] == "@import")
            {
                if (tokens.Length < 2)
                {
                    Console.Error.WriteLine("Expected symbol name after @import");
                    id = null;
                    return false;
                }
                if (tokens.Length > 2)
                {
                    Console.Error.WriteLine("Too many tokens after @import");
                    id = null;
                    return false;
                }

                var ret = new ImportDirective();
                ret.Label = tokens[1];
                id = ret;
                return true;
            }
            id = null;
            return false;
        }

        public override string ToString(int space)
        {
            string addrStr = "";
            if (Address.HasValue)
            {
                addrStr = $" ({Address.Value.ToString("X6")})";
            }
            string exportStr = "";
            if (ExporterFile != null)
            {
                exportStr = $" (defined in {ExporterFile})";
            }
            return $"@import {Label}{addrStr}{exportStr}";
        }
    }
}
