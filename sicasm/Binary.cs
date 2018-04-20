using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace SICXE
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
        /// Creates a listing file that represents the binary.
        /// </summary>
        /// <param name="path"></param>
        public void WriteLST(string path)
        {
            throw new NotSupportedException();
        }

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
                    writer.WriteLine(Environment.NewLine + "!");
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
            writer.WriteLine(Environment.NewLine + "!");

            writer.Dispose();
        }


        public override string ToString()
        {
            return $"{segments.Sum(s => s.Data.Count)} bytes in {segments.Count} segments";
        }

        // For debug.
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

        struct Interval
        {
            public int Start, Stop;
            public Interval(int start, int stop)
            {
                Start = start;
                Stop = stop;
            }
        }
    }
}
