﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXE
{
    // The code segment is always contiguous.
    public class Segment : IComparable<Segment>
    {
        public int BaseAddress
        { get; set; }

        public byte[] Data
        { get; set; }

        public int CompareTo(Segment other)
        {
            return BaseAddress.CompareTo(other.BaseAddress);
        }

        public override string ToString()
        {
            return $"{Data.Length}bytes@{BaseAddress.ToString("X")}";
        }
    }
}
