using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICXEAssembler
{
    static class StringExtensions
    {
        public static string[] SmartSplit(this string str)
        // Functions same as string.split(), except:
        // Does not split what is surrounded by quotation marks.
        // Aware of quotation marks that are escaped by a preceeding backslash.
        // Only splits once on contiguous whitespace.
        {
            const char QUOTATION_MARK = '\'';
            const char ESCAPE_CHARACTER = '\\';
            if (str.Length == 0)
                return new string[0];
            var ret = new List<string>();
            var current = new StringBuilder();
            bool inwhite = char.IsWhiteSpace(str[0]);
            bool insideliteral = false;
            for (int i = 0; i < str.Length; ++i)
            {
                char c = str[i];
                if (insideliteral)
                {
                    if (c == QUOTATION_MARK && (i > 0 || str[i - 1] != ESCAPE_CHARACTER))
                        insideliteral = false;
                    current.Append(c);
                }
                else
                {
                    if (c == QUOTATION_MARK && (i == 0 || str[i - 1] != ESCAPE_CHARACTER))
                    {
                        inwhite = false;
                        insideliteral = true;
                        current.Append(c);
                        continue;
                    }
                    if (char.IsWhiteSpace(c))
                    {
                        if (!inwhite)
                        {
                            // This is a transition to white.
                            ret.Add(current.ToString());
                            current.Clear();
                            inwhite = true;
                        }
                    }
                    else
                    {
                        inwhite = false;
                        current.Append(c);
                    }
                }
            }
            if (!inwhite && current.Length > 0)
            {
                ret.Add(current.ToString());
            }

            return ret.ToArray();
        }
    }
}
