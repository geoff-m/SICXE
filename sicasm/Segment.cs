using System;
using System.Collections.Generic;

namespace SICXE
{
    // The code segment is always contiguous.
    public class Segment : IComparable<Segment>
    {
        public int? BaseAddress
        { get; set; }

        public byte[] Data
        { get; set; }

        public int CompareTo(Segment other)
        {
            if (BaseAddress.HasValue)
            {
                if (other.BaseAddress.HasValue)
                {
                    return BaseAddress.Value.CompareTo(other.BaseAddress.Value);
                }
                return 1;
            }
            if (other.BaseAddress.HasValue)
                return -1;
            return 0; // Neither has value.
        }

        public override bool Equals(object obj)
        {
            Segment other = obj as Segment;
            if (other != null)
            {
                if (BaseAddress != other.BaseAddress)
                    return false;
                return Equals(Data, other.Data);
            }
            return false;
        }

        // This method is not used as of 3/7/2018. It is provided to quell warning.
        public override int GetHashCode()
        {
            if (BaseAddress.HasValue)
                return BaseAddress.Value;
            return -1;
        }

        public override string ToString()
        {
            if (BaseAddress.HasValue)
            {
                return $"{Data.Length}bytes@{BaseAddress.Value.ToString("X")}";
            }
            else
            {
                return $"{Data.Length}bytes@<not set>";
            }
        }
    }
}
