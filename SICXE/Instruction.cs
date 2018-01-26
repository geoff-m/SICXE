using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace SICXE
{
    enum Mnemonic
    {
        // Arithmetic
        ADD = 0x18,
        SUB = 0x1C,
        MUL = 0x20,
        DIV = 0x24,

        // Bitwise
        AND = 0x40,
        OR = 0x44,
        SHIFTL = 0xA4,
        SHIFTR = 0xA8,

        // Flow control
        J = 0x3C,
        JEQ = 0x30,
        JGT = 0x34,
        JLT = 0x38,
        JSUB = 0x48,
        RSUB = 0x4C,

        // Registers
        LDA = 0x00,
        LDL = 0x08,
        STA = 0x0C,
        STL = 0x14,
        STX = 0x10,
        CLEAR = 0xB4,

        // I/O
        RD = 0xD8,
        TD = 0xE0,
        WD = 0xDC,
        STCH = 0x54,

        // Other
        COMP = 0x28,
        TIX = 0x2C
    }

    enum Register // This will be cast to int to be stored in the same fields as addresses.
    {
        A = 1,
        B = 2,
        F = 3,
        L = 4,
        S = 5,
        T = 6,
        X = 7
    }

    enum OperandType
    {
        //NotSet = 0, // should always be set.
        Register = 1,
        Address = 2,
    }

    class Operand
    {
        public Operand(OperandType type)
        {
            Type = type;
            Value = null;
            AddressingMode = AddressingMode.NotSet;
        }
        public OperandType Type
        { get; private set; }
        public int? Value
        { get; set; }
        public AddressingMode AddressingMode
        { get; set; }
    }

    /// <summary>
    /// Represents a line in a program which has not been assembled.
    /// </summary>
    class Instruction
    {
        public IReadOnlyList<Operand> Operands
        { get; private set; }

        public Mnemonic Mnemonic
        { get; private set; }

        public Instruction(Mnemonic mnemonic)
        {
            Mnemonic = mnemonic;
            switch (mnemonic)
            {
                case Mnemonic.LDA:
                case Mnemonic.STA:
                case Mnemonic.ADD:
                case Mnemonic.COMP:
                    Operands = new List<Operand>() { new Operand(OperandType.Address) }.AsReadOnly();
                    break;
                case Mnemonic.CLEAR:
                    Operands = new List<Operand>() { new Operand(OperandType.Register) }.AsReadOnly();
                    break;
                default:
                    throw new NotSupportedException("That operation is not yet supported.");
            }
        }
    }
}
