using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace SICXE
{
    /// <summary>
    /// Represents a SIC/XE program that has not been assembled.
    /// </summary>
    class Program : IList<Instruction>
    {
        public Program()
        {
            prog = new List<Instruction>();
        }

        List<Instruction> prog;

        public int Count => ((IList<Instruction>)prog).Count;

        public bool IsReadOnly => ((IList<Instruction>)prog).IsReadOnly;

        public Instruction this[int index] { get => ((IList<Instruction>)prog)[index]; set => ((IList<Instruction>)prog)[index] = value; }

        public int IndexOf(Instruction item)
        {
            return ((IList<Instruction>)prog).IndexOf(item);
        }

        public void Insert(int index, Instruction item)
        {
            ((IList<Instruction>)prog).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<Instruction>)prog).RemoveAt(index);
        }

        public void Add(Instruction item)
        {
            ((IList<Instruction>)prog).Add(item);
        }

        public void Clear()
        {
            ((IList<Instruction>)prog).Clear();
        }

        public bool Contains(Instruction item)
        {
            return ((IList<Instruction>)prog).Contains(item);
        }

        public void CopyTo(Instruction[] array, int arrayIndex)
        {
            ((IList<Instruction>)prog).CopyTo(array, arrayIndex);
        }

        public bool Remove(Instruction item)
        {
            return ((IList<Instruction>)prog).Remove(item);
        }

        public IEnumerator<Instruction> GetEnumerator()
        {
            return ((IList<Instruction>)prog).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<Instruction>)prog).GetEnumerator();
        }
    }
}
