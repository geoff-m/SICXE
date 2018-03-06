using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace SICXE
{
    enum Register : byte // This will be cast to int to be stored in the same fields as addresses.
    {
        A = 0,
        X = 1,
        L = 2,
        B = 3,
        S = 4,
        T = 5,
        F = 6
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
        public string SymbolName
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
            bool hasSymbol = SymbolName != null;
            bool hasValue = Value != null;
            if (hasSymbol)
            {
                if (hasValue)
                {
                    return $"{prefix}{SymbolName}(={Value})";
                }
                return $"{prefix}{SymbolName}";

            }
            if (hasValue)
            {
                return $"{prefix}{Value}";
            }
            return $"{prefix}??";
        }

    }

    /// <summary>
    /// In a program, represents a line that contains a SIC/XE operation.
    /// </summary>
    class Instruction : Line
    {
        // typedef Mnemonic int
        // #define ADD 0x18

            
        public enum Mnemonic
        {
            // Arithmetic
            dog = 123,
            ADD = 0x18,
            ADDR = 0x90,
            SUB = 0x1C,
            SUBR = 0x94,
            MUL = 0x20,
            MULR = 0x98,
            DIV = 0x24,
            DIVR = 0x9C,

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
            LDB = 0x68,
            LDL = 0x08,
            LDS = 0x6C,
            LDT = 0x74,
            LDX = 0x04,
            STA = 0x0C,
            STB = 0x78,
            STL = 0x14,
            STS = 0X7C,
            STT = 0x84,
            STX = 0x10,
            CLEAR = 0xB4,
            RMO = 0xAC,
            LDCH = 0x50,
            STCH = 0x54,

            // I/O
            RD = 0xD8,
            TD = 0xE0,
            WD = 0xDC,

            // Other
            COMPR = 0xA0,
            COMP = 0x28,
            TIX = 0x2C,
            TIXR = 0xB8
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
                case Mnemonic.LDX:
                case Mnemonic.LDS:
                case Mnemonic.STA:
                case Mnemonic.STX:
                case Mnemonic.STS:
                case Mnemonic.ADD:
                case Mnemonic.SUB:
                case Mnemonic.MUL:
                case Mnemonic.COMP:
                case Mnemonic.JLT:
                case Mnemonic.TIX:
                    Operands = new List<Operand>() { new Operand(OperandType.Address) }.AsReadOnly();
                    Format = InstructionFormat.Format3;
                    break;
                case Mnemonic.CLEAR:
                    Operands = new List<Operand>() { new Operand(OperandType.Register) }.AsReadOnly();
                    Format = InstructionFormat.Format2;
                    break;
                case Mnemonic.RMO:
                case Mnemonic.ADDR:
                case Mnemonic.SUBR:
                case Mnemonic.COMPR:
                case Mnemonic.DIVR:
                case Mnemonic.MULR:
                case Mnemonic.TIXR:
                    Operands = new List<Operand>() { new Operand(OperandType.Register), new Operand(OperandType.Register) }.AsReadOnly();
                    Format = InstructionFormat.Format2;
                    break;
                case Mnemonic.RSUB:
                    Operands = new List<Operand>();
                    break;

                default:
                    throw new NotSupportedException("That operation is not yet supported.");
            }
        } // End constructor.

        /// <summary>
        /// Parses the given string into an operation.
        /// </summary>
        /// <param name="tokens">An array containing a mnemonic as well as any operands the instruction may have. The mnemonic may be have a prefix: * means SIC; + means Format 4.</param>
        /// <param name="result">If the parse was successful, an instruction with operations uninitialized. Otherwise, null.</param>
        /// <returns>A Boolean value indicating whether the parse succeeded.</returns>
        public static bool TryParse(string[] tokens, out Instruction result)
        {
            if (tokens.Length == 0 || tokens[0].Length == 0)
            {
                result = null;
                return false;
            }
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
            }
            if (removePrefix)
            {
                mnemonic = mnemonic.Substring(1);
            }

            if (!char.IsDigit(mnemonic[0]) && Enum.TryParse(mnemonic, true, out Mnemonic m)) // true to ignore case.
            {
                //if (m == Mnemonic.RMO)
                //    Debugger.Break();

                var ret = new Instruction(m);

                if (sic)
                    ret.Flags = 0;

                if (ret.Format == InstructionFormat.NotSet)
                {
                    if (fmt != InstructionFormat.NotSet)
                    {
                        ret.Format = fmt;
                    }
                }
                else
                {
                    if (fmt != InstructionFormat.NotSet)
                    {
                        // Instruction already knows its format but we planned to set it to something!
                        Console.WriteLine($"Instruction {m} must be format {(int)ret.Format} (prefix indicated format {(int)fmt})!");
                        result = null;
                        return false;
                    }
                }

                if (tokens.Length >= 2)
                {
                    var args = tokens[1];
                    int commaIdx = args.IndexOf(',');
                    if (commaIdx >= 0)
                    {
                        var afterComma = args.Substring(commaIdx + 1);
                        if (ret.Format == InstructionFormat.Format2)
                        {
                            if (ret.Operands.Count == 2)
                            {
                                var splitOnComma = new string[tokens.Length + 1];
                                splitOnComma[0] = tokens[0];
                                splitOnComma[1] = args.Substring(0, commaIdx);
                                splitOnComma[2] = args.Substring(commaIdx + 1);
                                Array.Copy(tokens, 2, splitOnComma, 3, tokens.Length - 2);
                                tokens = splitOnComma;
                            }
                        }
                        else
                        {
                            if (afterComma != "x")
                            {
                                Console.WriteLine($"Unexpected ',' in {string.Join(" ", tokens)}");
                                result = null;
                                return false;
                            }
                        }
                    }
                }

                int operandCount = ret.Operands.Count;
                //if (tokens.Length - 1 != operandCount)  Extra tokens are usually just comments, so this warning is pretty useless.
                //{
                //    Debug.WriteLine($"Warning: Operation {mnemonic.ToString()} takes {operandCount} operands but {tokens.Length - 1} were given.");
                //    // In this method, this isn't a showstopper-- we just ignore extra tokens, or leave operands null if there aren't enough.
                //    // Could be comment.
                //}

                // Copy in all the operands.
                for (int argIdx = 1; argIdx < tokens.Length && argIdx <= operandCount; ++argIdx)
                {
                    var operand = ret.Operands[argIdx - 1];
                    var token = tokens[argIdx];
                    //todo: switch to something like this instead if this foreach ...if (operand.Type == OperandType.)

                    // Parse the operand as the type we expect.
                    switch (operand.Type)
                    {
                        case OperandType.Device:
                        case OperandType.Address:
                            // Acceptable formats:
                            //  Number.
                            //  Number with either @ or # prefix.
                            //  Symbol (any alphanumeric string).
                            switch (token[0])
                            {
                                case '@':
                                    operand.AddressingMode = AddressingMode.Indirect;
                                    token = token.Substring(1);
                                    break;
                                case '#':
                                    operand.AddressingMode = AddressingMode.Immediate;
                                    token = token.Substring(1);
                                    break;
                            }

                            // Interpret the remainder of the token as an address, if possible, or else a symbol.
                            if (int.TryParse(token, out int addr))
                            {
                                operand.Value = addr;
                            }
                            else
                            {
                                // todo: check that token is valid for a symbol name and return false if it isn't.
                                operand.SymbolName = token;
                            }
                            break;
                        case OperandType.Register:
                            if (Enum.TryParse(token, true, out Register reg))
                            {
                                operand.Value = (int)reg; // Casting Register to int.
                            }
                            else
                            {
                                Console.WriteLine($"Could not parse {token} as a register.");
                                result = null;
                                return false;
                            }
                            break;
                    }

                    Debug.WriteLine($"Parsed {token} as {operand.Type.ToString()}.");

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
