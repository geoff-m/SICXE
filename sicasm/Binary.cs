using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace SICXEAssembler
{
    /// <summary>
    /// Represents a SIC/XE program that has been assembled.
    /// </summary>
    public class Binary
    {
        public int EntryPoint
        { get; set; }

        SortedSet<Segment> segments = new SortedSet<Segment>();
        public Segment[] Segments { get { return segments.ToArray(); } }

        public bool AddSegment(Segment s) { return segments.Add(s); }

        /// <summary>
        /// Creates an OBJ file for loading into a SIC/XE machine.
        /// </summary>
        /// <param name="path"></param>
        public void WriteOBJ(string path)
        {
            var writer = new StreamWriter(path, false);
            var last = segments.Last();
            foreach (var seg in Segments)
            {
                if (seg != last)
                {
                    Debug.WriteLine($"\nbegin non-final segment {seg.ToString()}");
                    Debug.WriteLine(seg.BaseAddress.Value.ToString("X6"));
                    Debug.WriteLine("000000");
                    writer.WriteLine(seg.BaseAddress.Value.ToString("X6"));
                    writer.WriteLine("000000");
                    foreach (byte b in seg.Data)
                    {
                        Debug.Write(b.ToString("X2"));
                        writer.Write(b.ToString("X2"));
                    }
                    if (seg.Data.Count > 0)
                        writer.WriteLine();
                    writer.WriteLine("!");
                }
            }
            Debug.WriteLine($"\nbegin final segment {last.ToString()}");
            Debug.WriteLine(last.BaseAddress.Value.ToString("X6"));
            writer.WriteLine(last.BaseAddress.Value.ToString("X6"));
            Debug.WriteLine(EntryPoint.ToString("X6"));
            writer.WriteLine(EntryPoint.ToString("X6"));
            foreach (byte b in last.Data)
            {
                Debug.Write(b.ToString("X2"));
                writer.Write(b.ToString("X2"));
            }
            if (last.Data.Count > 0)
                writer.WriteLine();
            writer.WriteLine("!");

            writer.Dispose();
        }


        public override string ToString()
        {
            return $"{segments.Sum(s => s.Data.Count)} bytes in {segments.Count} segments";
        }

        // For debug. Should always be true.
        // Checks whether this Binary's bytes are contiguous across all segments.
        public bool IsContiguous()
        {
            if (segments.Count < 2)
                return true;
            var intervals = segments.Select(s => new Interval(s.BaseAddress.Value, s.BaseAddress.Value + s.Data.Count)).ToList();
            intervals.Sort((x, y) => x.Start.CompareTo(y.Start));
            Interval last = intervals[0];
            for (int i = 1; i < intervals.Count; ++i)
            {
                Interval current = intervals[i];
                // Measure the gap between this one's start and last one's stop.
                int gap = current.Start - last.Stop;
                if (gap != 1)
                    return false;
                last = current;
            }
            return true;
        }


        public Interval GetMaximumRange()
        {
            int myLowest = int.MaxValue;
            int myHighest = 0;
            foreach (var s in Segments)
            {
                int sbase = s.BaseAddress.Value;
                if (sbase < myLowest)
                    myLowest = sbase;
                int shigh = sbase + s.Data.Count;
                if (shigh > myHighest)
                    myHighest = shigh;
            }
            return new Interval(myLowest, myHighest);
        }

        public bool CanCombineWithoutRelocating(Binary other)
        {
            var myRange = GetMaximumRange();
            var otherRange = other.GetMaximumRange();
            return !myRange.Overlaps(otherRange);
        }

        public Tuple<Segment, Segment> FindCollidingSegments(Binary other)
        {
            // Stupid O(n * m) method.
            foreach (var myseg in Segments)
            {
                foreach (var otherseg in other.Segments)
                {
                    if (myseg.BaseAddress < otherseg.BaseAddress)
                    {
                        if (myseg.Data.Count >= otherseg.BaseAddress) // Other begins inside me.
                            return new Tuple<Segment, Segment>(myseg, otherseg);
                    }
                    else
                    {
                        if (myseg.BaseAddress <= otherseg.Data.Count) // I begin inside other.
                            return new Tuple<Segment, Segment>(myseg, otherseg);
                    }
                }
            }
            return null;
            //return FindCollidingSegments(other, 0);
        }

        /*/// <summary>
        /// Finds a pair of Segments, one from each Binary, that collide near the given address.
        /// </summary>
        /// <param name="other">The Binary to check for collisions with.</param>
        /// <param name="address">A guess at the address of the collision.</param>
        /// <returns>A tuple of Segments: the first from this Binary, the second from the other.</returns>
        public Tuple<Segment, Segment> FindCollidingSegments(Binary other, int address)
        {
            Segments.g
            foreach (var myseg in Segments)
            {
                if (myseg.BaseAddress >= 
            }
        }*/
    }
}
