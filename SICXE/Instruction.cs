using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SICXE
{

    class Instruction
    {
        public Operation Operation
        { get; private set; }
        public int Argument1
        { get; private set; }
        public int Argument2
        { get; private set; }
        public AddressingMode AddressingMode
        { get; private set; }

        public Instruction(Operation op) // used for nullary operations
        {
            Operation = op;
            AddressingMode = AddressingMode.NotSet;
            switch (op)
            {
                default:
                    throw new ArgumentException($"Operation {op.ToString()} cannot accept 0 arguments!");
            }
        }

        public Instruction(Operation op, int arg1, AddressingMode mode) // used for unary operations
        {
            Operation = op;
            AddressingMode = mode;
            Argument1 = arg1;
            switch (op)
            {
                case Operation.ADD:
                case Operation.SUB:
                case Operation.MUL:
                case Operation.DIV:
                case Operation.LDA:
                case Operation.STA:
                    break;

                default:
                    throw new ArgumentException($"Operation {op.ToString()} cannot accept 1 argument!");
            }


        }

        public Instruction(Operation op, int arg1, int arg2, AddressingMode mode) // used for binary operations
        {
            Operation = op;
            AddressingMode = mode;
            Argument1 = arg1;
            Argument2 = arg2;
            switch (op)
            {
                default:
                    throw new ArgumentException($"Operation {op.ToString()} cannot accept 2 arguments!");
            }
        }

        public static bool TryParse(string str, out Instruction inst)
        {
            var tokens = SmartSplit(str);
            if (!Enum.TryParse(tokens[0], true, out Operation op))
            {
                inst = null;
                return false; // Unrecognized operation.
            }
            switch (op)
            {
                case Operation.LDA:
                    if (tokens.Length != 2)
                    {
                        inst = null;
                        return false;
                    }

                    break;
            }
        }

        /// <summary>
        /// Parses a token like A+#5 into an address.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="baseAddress"></param>
        /// <param name="offset"></param>
        /// <param name="mode"></param>
        /// <returns>True on success.</returns>
        private static bool TryParseReference(string str, out int baseAddress, out int offset, out AddressingMode mode)
        {
            /* Supported formats:   [register]
             *                      #[immediate]
             *                      [register]+#[immediate]
             *                      L+[register]
             *                      L+#[immediate]
             */
            for (int i=0; i<str.Length; ++i)
            {
                
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
    }
}
