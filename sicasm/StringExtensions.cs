using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SICXE
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
                    if (c == QUOTATION_MARK && (i > 0 || str[i - 1] != '\\'))
                    {
                        insideliteral = false;
                    }
                    else
                    {
                        //Debug.WriteLine("literal: " + current.ToString());
                    }
                    current.Append(c);
                }
                else
                {
                    if (c == QUOTATION_MARK && (i == 0 || str[i - 1] != '\\'))
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
                            //Debug.WriteLine("pushing " + current.ToString());
                            ret.Add(current.ToString());
                            current.Clear();
                            inwhite = true;
                        }
                    }
                    else
                    {
                        inwhite = false;
                        current.Append(c);
                        //Debug.WriteLine("cat name: " + current.ToString());
                    }
                }
            }
            if (!inwhite && current.Length > 0)
            {
                //Debug.WriteLine("pushing " + current.ToString());
                ret.Add(current.ToString());
            }

            return ret.ToArray();
        }
    }
}
