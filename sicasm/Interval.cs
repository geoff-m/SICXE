using System;

namespace SICXEAssembler
{
    public struct Interval
    {
        public readonly int Start, Stop;
        public Interval(int start, int stop)
        {
            Start = start;
            Stop = stop;
        }

        public bool Overlaps(Interval other)
        {
            if (Start < other.Start)
            {
                if (Stop < other.Start)
                    return false;
                return true;
            }
            if (Start > other.Stop)
                return false;
            return true;
        }

        public override string ToString()
        {
            return $"{Start}~{Stop} (size={Stop - Start})";
        }
    }
}
