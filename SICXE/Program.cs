using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace SICXE
{
    /// <summary>
    /// Represents a SIC/XE program that has not been assembled.
    /// </summary>
    class Program : IList<Line>
    {
        public Program()
        {
            prog = new List<Line>();
        }

        List<Line> prog;

        public int Count => ((IList<Line>)prog).Count;

        public bool IsReadOnly => ((IList<Line>)prog).IsReadOnly;

        public Line this[int index] { get => ((IList<Line>)prog)[index]; set => ((IList<Line>)prog)[index] = value; }

        public int IndexOf(Line item)
        {
            return ((IList<Line>)prog).IndexOf(item);
        }

        public void Insert(int index, Line item)
        {
            ((IList<Line>)prog).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<Line>)prog).RemoveAt(index);
        }

        public void Add(Line item)
        {
            ((IList<Line>)prog).Add(item);
        }

        public void Clear()
        {
            ((IList<Line>)prog).Clear();
        }

        public bool Contains(Line item)
        {
            return ((IList<Line>)prog).Contains(item);
        }

        public void CopyTo(Line[] array, int arrayIndex)
        {
            ((IList<Line>)prog).CopyTo(array, arrayIndex);
        }

        public bool Remove(Line item)
        {
            return ((IList<Line>)prog).Remove(item);
        }

        public IEnumerator<Line> GetEnumerator()
        {
            return ((IList<Line>)prog).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<Line>)prog).GetEnumerator();
        }
    }
}
