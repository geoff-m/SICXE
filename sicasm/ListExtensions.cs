using System;
using System.Collections.Generic;
using System.Linq;


namespace SICXEAssembler
{
    public static class ListExtensions
    {
        public static T[] Join<T>(this IList<T[]> list)
        {
            var ret = new List<T>();
            foreach (var piece in list)
            {
                ret.AddRange(piece);
            }
            return ret.ToArray();
        }
    }
}
