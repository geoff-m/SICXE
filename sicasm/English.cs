using System;

namespace SICXEAssembler
{
    internal static class English
    {
        public static string Pluralize(int count, string noun)
        {
            if (count == 1)
                return noun;
            if (noun.EndsWith("y"))
            {
                return noun.Substring(0, noun.Length - 1) + "ies";
            }
            return noun + "s";
        }
    }
}
