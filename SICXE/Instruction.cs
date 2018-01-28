using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace SICXE
{
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
        Register = 1,
        Address = 2,
        Device = 4
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
        public AddressingMode AddressingMode
        { get; set; }
        public int? Value
        { get; set; }

        /// <summary>
        /// Gets or sets a string that acts as a placeholder for a real value. For help in assembly.
        /// </summary>
        public string Symbol
        { get; set; }

        public override string ToString()
        {
            string prefix;
            switch (AddressingMode)
            {
                case AddressingMode.Immediate:
                    prefix = "#";
                    break;
                case AddressingMode.Indirect:
                    prefix = "@";
                    break;
                default:
                    prefix = "";
                    break;
            }
            bool hasSymbol = Symbol != null;
            bool hasValue = Value != null;
            if (hasSymbol)
            {
                if (hasValue)
                {
                    return $"{prefix}{Symbol}({Value})";
                }
                return $"{prefix}{Symbol}";

            }
            if (hasValue)
            {
                return $"{prefix}{Value}";
            }
            return $"{prefix} ????";
        }

    }

    /// <summary>
    /// In a program, represents a line that contains a SIC/XE operation.
    /// </summary>
    class Instruction : Line
    {
        public enum Mnemonic
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

        public enum Flag : int
        {
            N = 0x1,
            I = 0x2,
            X = 0x4,
            B = 0x8,
            P = 0x10,
            E = 0x20
        }

        public Flag? Flags
        { get; set; }

        public IReadOnlyList<Operand> Operands
        { get; private set; }

        public Mnemonic Operation
        { get; private set; }

        public InstructionFormat Format
        { get; set; }

        public Instruction(Mnemonic mnemonic)
        {
            Operation = mnemonic;
            switch (mnemonic)
            {
                case Mnemonic.LDA:
                case Mnemonic.STA:
                case Mnemonic.ADD:
                case Mnemonic.COMP:
                    Operands = new List<Operand>() { new Operand(OperandType.Address | OperandType.Register) }.AsReadOnly();
                    break;
                case Mnemonic.CLEAR:
                    Operands = new List<Operand>() { new Operand(OperandType.Register) }.AsReadOnly();
                    break;
                default:
                    throw new NotSupportedException("That operation is not yet supported.");
            }
        }

        /// <summary>
        /// Parses the given string into an operation.
        /// </summary>
        /// <param name="tokens">An array containing a mnemonic as well as any operands the instruction may have. The mnemonic may be have a prefix: * means SIC; + means Format 4.</param>
        /// <param name="result">If the parse was successful, an instruction with operations uninitialized. Otherwise, null.</param>
        /// <returns>A Boolean value indicating whether the parse succeeded.</returns>
        public static bool TryParse(string[] tokens, out Instruction result)
        {
            string mnemonic = tokens[0];
            bool removePrefix = false;
            bool sic = false;
            InstructionFormat fmt = InstructionFormat.NotSet;
            switch (mnemonic[0])
            {
                case '*':
                    // This is a SIC instruction.
                    // Means all flags are low. (Right?)
                    sic = true;
                    removePrefix = true;
                    break;
                case '+':
                    fmt = InstructionFormat.Format4;
                    removePrefix = true;
                    break;

                    // todo: any other cases...
            }
            if (removePrefix)
            {
                mnemonic = mnemonic.Substring(1);
            }
            if (Enum.TryParse(mnemonic, true, out Mnemonic m)) // true to ignore case.
            {
                var ret = new Instruction(m);

                if (sic)
                    ret.Flags = 0;

                ret.Format = fmt;


                int operandCount = ret.Operands.Count;
                if (tokens.Length - 1 != operandCount)
                {
                    Debug.WriteLine($"Warning: Operation {mnemonic.ToString()} takes {operandCount} operands but {tokens.Length - 1} were given.");
                    // In this method, this isn't a showstopper-- we just ignore extra tokens, or leave operands null if there aren't enough.
                }

                // Copy in all the operands.
                for (int argIdx = 1; argIdx < tokens.Length && argIdx <= operandCount; ++argIdx)
                {
                    var operand = ret.Operands[argIdx - 1];
                    var token = tokens[argIdx];
                    // Operand string may be a number, a register or device, or a symbol.
                    // Attempt to parse this operand in all possible ways until we succeed.
                    foreach (OperandType operandType in Enum.GetValues(typeof(OperandType)))
                    {
                        bool success = false;
                        switch (operandType)
                        {

                            case OperandType.Register:
                                if (Enum.TryParse(token, true, out Register reg))
                                {
                                    operand.Value = (int)reg; // Casting Register to int.
                                    success = true;
                                }
                                break;

                            case OperandType.Device:
                                // todo: support devices.
                                // create enum for devices that maps to same type as address (int).
                                // parse as device.
                                break;

                            case OperandType.Address:
                                switch (token[0])
                                {
                                    case '#':
                                        operand.AddressingMode = AddressingMode.Immediate;
                                        token = token.Substring(1);
                                        break;
                                    case '@':
                                        operand.AddressingMode = AddressingMode.Indirect;
                                        token = token.Substring(1);
                                        break;
                                    // Todo: handle any other prefixes...
                                    default:
                                        operand.AddressingMode = AddressingMode.Simple;
                                        break;
                                }

                                if (uint.TryParse(token, out uint addr))
                                {
                                    operand.Value = (int)addr;
                                }
                                else
                                {
                                    operand.Symbol = token;
                                }
                                break;
                        }
                        if (success)
                        {
                            Debug.WriteLine($"Parsed {token} as {operandType.ToString()}.");
                            break;
                        }
                    }
                } // for each operand.

                result = ret;
                return true;
            }

            result = null;
            return false;
        }

        public override string ToString()
        {
            if (Label != null)
            {
                return $"{Label}: {Operation.ToString()} {string.Join(",", Operands)}";
            }
            return $"{Operation.ToString()} {string.Join(",", Operands)}";
        }
    }
}
