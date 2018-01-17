using System;
using System.Collections.Generic;
using System.Linq;

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
    }
}
