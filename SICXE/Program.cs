using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Collections;

namespace SICXE
{
    /// <summary>
    /// Represents a SIC/XE program that has not been assembled.
    /// </summary>
    class Program : IList<Instruction>
    {
        public static Program FromFile(string path)
        {
            StreamReader read = null;
            try
            {
                read = new StreamReader(path);
                while (!read.EndOfStream)
                {
                    var line = read.ReadLine();
                    // Strip comments.
                    int commentStart = line.IndexOf(';');
                    if (commentStart > 0)
                    {
                        line = line.Substring(0, line.Length - commentStart);
                        line = line.Trim();
                        var tokens = SmartSplit(line);
                        
                    }
                }
            }
            finally
            {
                if (read != null)
                    read.Dispose();
            }
        }

        private static string[] SmartSplit(string str)
        // Functions same as string.split(), except:
        // Does not split what is surrounded by quotation marks
        // Aware of \" escaped quotation marks.
        // Only splits once on contiguous whitespace.
        {
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
                    if (c == '"' && (i > 0 || str[i - 1] != '\\'))
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
                    if (c == '"' && (i == 0 || str[i - 1] != '\\'))
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
