using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace SICXEAssembler
{
    public enum Register : byte // This will be cast to int to be stored in the same fields as addresses.
    {
        A = 0, // source: book page 5 (pdf page 21)
        X = 1,
        L = 2,
        B = 3, // source: book page 7 (pdf page 23)
        S = 4,
        T = 5,
        F = 6,
        PC = 8,
        SW = 9
    }

    /// <summary>
    /// Indicates the length--in bytes--of an assembled instruction.
    /// </summary>
    public enum InstructionFormat
    {
        NotSet = 0,
        Format1 = 1,
        Format2 = 2,
        Format3 = 3,
        Format4 = 4,
        Format3Or4 = 12
    }

    public enum OperandType
    {
        Register = 1,
        Address = 2,
    }

    public class Operand
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
#if DEBUG
                if (hasValue)
                {
                    return $"{prefix}{SymbolName}(={Value.Value.ToString("X")})";
                }
#endif
                return $"{prefix}{SymbolName}";
            }
            if (hasValue)
            {
                if (Type == OperandType.Register)
                    return $"{prefix}{(Register)Value}";
                return $"{prefix}{Value}";
            }
            return $"{prefix}??";
        }

    }

    public static class MnemonicExtensions
    {
        public static bool IsJump(this Instruction.Mnemonic value)
        {
            switch (value)
            {
                case Instruction.Mnemonic.J:
                case Instruction.Mnemonic.JEQ:
                case Instruction.Mnemonic.JGT:
                case Instruction.Mnemonic.JLT:
                    return true;
                default:
                    return false;
            }
        }
    }

    [FlagsAttribute]
    public enum InstructionFlags : byte
    {
        None = 0,
        N = 0x1,
        I = 0x2,
        X = 0x4,
        B = 0x8,
        P = 0x10,
        E = 0x20
    }

    /// <summary>
    /// In a program, represents a line that contains a SIC/XE operation.
    /// </summary>
    public class Instruction : Line
    {

        public enum Mnemonic : byte
        {
            // Arithmetic
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
            TIXR = 0xB8,
            NOP = 0xFF
        }

        /// <summary>
        /// Tests whether the given byte can begin a known opcode. If it can, the associated mnemonic is returned. Otherwise, null is returned.
        /// </summary>
        /// <param name="b">The byte to parse. For format 3/4 instructions, the lower 2 bits are ignored.</param>
        /// <returns></returns>
        public static Mnemonic? ParseMnemonic(byte b)
        {
            // Check all 8 bits (formats 1 and 2).
            if (Enum.IsDefined(typeof(Mnemonic), b))
                return (Mnemonic)b;

            // Check upper 6 bits (formats 3 and 4).
            var firstSextet = (byte)(b & 0xfc);
            if (Enum.IsDefined(typeof(Mnemonic), firstSextet))
                return (Mnemonic)firstSextet;

            // Unrecognized.
            return null;
        }

        public InstructionFlags Flags
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
                case Mnemonic.LDT:
                case Mnemonic.LDB:
                case Mnemonic.LDL:
                case Mnemonic.LDCH:
                case Mnemonic.STCH:
                case Mnemonic.STA:
                case Mnemonic.STX:
                case Mnemonic.STS:
                case Mnemonic.STT:
                case Mnemonic.STB:
                case Mnemonic.STL:
                case Mnemonic.ADD:
                case Mnemonic.SUB:
                case Mnemonic.MUL:
                case Mnemonic.DIV:
                case Mnemonic.AND:
                case Mnemonic.OR:
                case Mnemonic.COMP:
                case Mnemonic.J:
                case Mnemonic.JSUB:
                case Mnemonic.JLT:
                case Mnemonic.JGT:
                case Mnemonic.JEQ:
                case Mnemonic.TIX:
                case Mnemonic.TD:
                case Mnemonic.WD:
                case Mnemonic.RD:
                    Operands = new List<Operand>() { new Operand(OperandType.Address) }.AsReadOnly();
                    Format = InstructionFormat.Format3Or4;
                    break;
                case Mnemonic.CLEAR:
                case Mnemonic.TIXR:
                    Operands = new List<Operand>() { new Operand(OperandType.Register) }.AsReadOnly();
                    Format = InstructionFormat.Format2;
                    break;
                case Mnemonic.RMO:
                case Mnemonic.ADDR:
                case Mnemonic.SUBR:
                case Mnemonic.COMPR:
                case Mnemonic.DIVR:
                case Mnemonic.MULR:
                    Operands = new List<Operand>() { new Operand(OperandType.Register), new Operand(OperandType.Register) }.AsReadOnly();
                    Format = InstructionFormat.Format2;
                    break;
                case Mnemonic.SHIFTL:
                case Mnemonic.SHIFTR:
                    // The second operand isn't really an address, but we mark it that way so that it isn't ToStringged as a register.
                    // For example, we don't want to print "SHIFTR A,PC" instead of "SHIFTR A,8".
                    Operands = new List<Operand>() { new Operand(OperandType.Register), new Operand(OperandType.Address) }.AsReadOnly();
                    Format = InstructionFormat.Format2;
                    break;
                case Mnemonic.RSUB:
                    Operands = new List<Operand>();
                    Format = InstructionFormat.Format3Or4;
                    break;
                case Mnemonic.NOP:
                    Operands = new List<Operand>();
                    Format = InstructionFormat.Format1;
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
            if (mnemonic.Length < 1)
            {
                result = null;
                return false;
            }

            Mnemonic m;
            if (char.IsDigit(mnemonic[0]) || !Enum.TryParse(mnemonic, true, out m)) // true to ignore case.
            {
                result = null;
                return false;
            }

            var ret = new Instruction(m);
            //if (m == Mnemonic.CLEAR)
            //    Debugger.Break();

            if (sic)
                ret.Flags = 0;

            if (ret.Format == InstructionFormat.NotSet || (ret.Format == InstructionFormat.Format3Or4 && fmt == InstructionFormat.Format4))
            {
                ret.Format = fmt;
            }
            else
            {
                if (fmt != InstructionFormat.NotSet)
                {
                    // Instruction already knows its format but we planned to set it to something!
                    Console.Error.WriteLine($"Instruction {m} must be format {(int)ret.Format} (prefix indicated format {(int)fmt})!");
                    result = null;
                    return false;
                }
            }

            // If format 4 has not been indicated by this point, assume 3/4 instructions are 3.
            if (ret.Format == InstructionFormat.Format3Or4)
                ret.Format = InstructionFormat.Format3;

            Debug.Assert(ret.Format != InstructionFormat.NotSet, "Instruction's format should be set by this point.");

            bool isIndexed = false;
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
                        isIndexed = false;
                    }
                    else
                    {
                        if (afterComma != "x" && afterComma != "X")
                        {
                            Console.Error.WriteLine($"Unexpected ',' in {string.Join(" ", tokens)}");
                            result = null;
                            return false;
                        }
                        isIndexed = true;
                    }
                }
            }

            // Copy in all the operands by parsing as many tokens as we need.
            // Extra tokens are usually just comments. In this method, we simply ignore them, or leave operands as null if there aren't enough.
            int tokenIdx;
            int operandCount = ret.Operands.Count;
            for (tokenIdx = 1; tokenIdx < tokens.Length && tokenIdx <= operandCount; ++tokenIdx)
            {
                var operand = ret.Operands[tokenIdx - 1];
                var token = tokens[tokenIdx];

                // Parse the operand as the type we expect.
                switch (operand.Type)
                {
                    //case OperandType.Device:
                    case OperandType.Address:
                        // Acceptable formats:
                        //  Number.
                        //  Number with either @ or # prefix.
                        //  Symbol (most any string?)
                        //  Literal (=C followed by any string surrounded by ', or =X followed by any even-length hex string surrounded by ').
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
                        int addr;
                        if (int.TryParse(token, out addr))
                        {
                            if (ret.Operation == Mnemonic.SHIFTL || ret.Operation == Mnemonic.SHIFTR)
                            {
                                if (addr == 0)
                                {
                                    Console.Error.WriteLine("Shift by zero is not allowed.");
                                    result = null;
                                    return false;
                                }
                                // The book says to subtract one.
                                // Osprey's sicasm does it too.
                                operand.Value = addr - 1; 
                            } else
                            {
                                operand.Value = addr;
                            }
                        }
                        else
                        {
                            // todo: check that token is valid for a symbol name and return false if it isn't.
                            operand.SymbolName = token;
                        }
                        if (isIndexed)
                            operand.AddressingMode |= AddressingMode.Indexed;
                        break;
                    case OperandType.Register:
                        Register reg;
                        if (Enum.TryParse(token, true, out reg))
                        {
                            operand.Value = (int)reg; // Casting Register to int.
                        }
                        else
                        {
                            Console.Error.WriteLine($"Could not parse {token} as a register.");
                            result = null;
                            return false;
                        }
                        break;
                }

                Debug.WriteLine($"Parsed {token} as {operand.Type.ToString()}.");
            } // for each operand.

            ret.Comment = string.Join(" ", tokens, tokenIdx, tokens.Length - tokenIdx);

            result = ret;
            return true;
        }

        public override string ToString()
        {
            return ToString(1);
            string prefix;
            if (Format == InstructionFormat.Format4)
                prefix = "+";
            else
                prefix = "";
#if DEBUG
            if (Label != null)
                return $"{Label}: {prefix}{Operation.ToString()} {string.Join(",", Operands)}";
            return $"{prefix}{Operation.ToString()} {string.Join(",", Operands)}";
#else
            if (Label != null)
                return $"{Label}\t{prefix}{Operation.ToString()} {string.Join(",", Operands)}";
            return $"\t\t{prefix}{Operation.ToString()} {string.Join(",", Operands)}";
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="space">The number of spaces to insert after the label, if it is not null.</param>
        /// <returns></returns>
        public override string ToString(int space)
        {
            string prefix;
            if (Format == InstructionFormat.Format4)
                prefix = "+";
            else
                prefix = "";
            if (Label != null)
                //prefix = $"{Label}{new string(' ', space - Label.Length + 2)}{prefix}";
                prefix = $"{Label}{new string(' ', space)}{prefix}";
            //else
            //prefix = $"{new string(' ', space + 2)}{prefix}";
            if (Operands.Count == 1 && Operands[0].Type == OperandType.Address)
            {
                if (Flags == 0)
                    return $"{prefix}{Operation} {Operands[0]}";
                var f = Flags;
                var operandPrefix = "";
                var operandSuffix = "";

                bool operandPositive = true;
                bool knowOperand = Operands[0].Value.HasValue;
                int opval = 0;

                if (knowOperand)
                {
                    opval = Operands[0].Value.Value;
                    if (Format == InstructionFormat.Format4)
                    {
                        opval = Decode20BitTwosComplement(opval, out operandPositive);
                    }
                    else
                    {
                        opval = Decode12BitTwosComplement(opval, out operandPositive);
                    }
                }

                if (f.HasFlag(InstructionFlags.P))
                    operandPrefix = operandPositive ? "P+" : "P-";
                if (f.HasFlag(InstructionFlags.B))
                    operandPrefix = operandPositive ? "B+" : "B-";
                bool nFlag = f.HasFlag(InstructionFlags.N);
                bool iFlag = f.HasFlag(InstructionFlags.I);
                if (nFlag ^ iFlag)
                {
                    if (nFlag)
                        operandPrefix = "@" + operandPrefix;
                    if (iFlag)
                        operandPrefix = "#" + operandPrefix;
                }

                if (f.HasFlag(InstructionFlags.X))
                    operandSuffix = ",X";

                string operandFormatString = Format == InstructionFormat.Format4 ? "X4" : "X3";
                if (knowOperand)
                {
                    return $"{prefix}{Operation} {operandPrefix}0x{opval.ToString(operandFormatString)}{operandSuffix}";
                }
                else if (Operands[0].ToString().Length > 0)
                {
                    return $"{prefix}{Operation} {operandPrefix}{Operands[0]}{operandSuffix}";
                }
                else
                {
                    return $"{prefix}{Operation} {operandPrefix}0x??????{operandSuffix}";
                }
            }

            var sb = new StringBuilder();
            sb.Append($"{prefix}{Operation}");
            if (Operands.Count == 0)
                return sb.ToString();
            sb.Append(' ');
            sb.Append(Operands[0].Type == OperandType.Address ? Operands[0].ToString() : Enum.GetName(typeof(Register), Operands[0].Value));
            if (Operands.Count == 2)
            {
                if (Operation == Mnemonic.SHIFTL || Operation == Mnemonic.SHIFTR)
                {
                    sb.AppendFormat(",{0}", (Operands[1].Value + 1).ToString()); // +1 because we subtracted 1 from the value in the source program.
                                                                           // We did that because the book said to. And osprey's sicasm does it.
                }
                else
                    sb.AppendFormat(",{0}", Operands[1].Type == OperandType.Address ? Operands[1].ToString() : Enum.GetName(typeof(Register), Operands[1].Value));
            }
                
            return sb.ToString();
        }

        public static int Decode12BitTwosComplement(int n, out bool positive)
        {
            if ((n & (1 << 11)) != 0)
            {
                // Number is negative.
                positive = false;
                n = ~n + 1;
                return n & 0xfff;
            }
            positive = true;
            return n;
        }

        public static int Decode20BitTwosComplement(int n, out bool positive)
        {
            if ((n & (1 << 19)) != 0)
            {
                // Number is negative.
                positive = false;
                n = ~n + 1;
                return n & 0xfffff;
            }
            positive = true;
            return n;
        }

        public static bool IsJump(Mnemonic op)
        {
            switch (op)
            {
                case Mnemonic.J:
                case Mnemonic.JEQ:
                case Mnemonic.JLT:
                case Mnemonic.JGT:
                    return true;
            }
            return false;
        }
    }
}
