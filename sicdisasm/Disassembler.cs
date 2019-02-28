using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using SICXEAssembler;

namespace sicdisasm
{
    public class DisassemblyResult
    {
        private List<Instruction> instrs;
        public IReadOnlyList<Instruction> Instructions
        {
            get { return instrs.AsReadOnly(); }
        }

        internal DisassemblyResult()
        {
            instrs = new List<Instruction>();
        }

        internal void AddInstruction(Instruction instr)
        {
            instrs.Add(instr);
        }

        public bool FindInstruction(int address, out int index)
        {
            var addr = new Instruction(Instruction.Mnemonic.ADD);
            addr.Address = address;
            index = instrs.BinarySearch(addr, new InstructionAddressMatcher());
            return index >= 0;
        }

        class InstructionAddressMatcher : IComparer<Instruction>
        {
            public int Compare(Instruction x, Instruction y)
            {
                if (x == null)
                {
                    if (y == null)
                        return 0;
                    return y.Address.Value;
                }
                if (y == null)
                    return x.Address.Value;
                // probably need more null checks here
                return x.Address.Value.CompareTo(y.Address.Value);
            }
        }
    }

    public class Disassembler
    {
        // this class is actually stateless...

        public DisassemblyResult DisassembleWithContinue(byte[] data)
        {
            return DisassembleWithContinue(new MemoryStream(data), 0, data.Length);
        }

        //public DisassemblyResult DisassembleWithContinue(byte[] data, int offset, int length)
        //{
        //    return DisassembleWithContinue(new MemoryStream(data, offset, length), 0, length);
        //}

        /// <summary>
        /// Disassembles the data stream in the specified region, continuing on error until the next valid instruction is found.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset">The index in the stream to begin reading.</param>
        /// <param name="length">The maximum number of bytes to read from the stream.</param>
        /// <returns></returns>
        public DisassemblyResult DisassembleWithContinue(Stream data, int offset, int length)
        {
            data.Seek(offset, SeekOrigin.Begin);

            var ret = new DisassemblyResult();
            Instruction nextInstr;
            while (data.Position < offset + length)
            {
                nextInstr = TryDisassembleInstruction(data, length);
                if (nextInstr != null)
                {
                    ret.AddInstruction(nextInstr);
                }
            }
            return ret;
        }

        public DisassemblyResult Disassemble(byte[] data)
        {
            return Disassemble(new MemoryStream(data), 0, data.Length);
        }

        public static bool TryDisassembleAbsoluteJumpTarget(Instruction instr, out int address)
        {
            var op = instr.Operation;
            if (!Instruction.IsJump(op) && op != Instruction.Mnemonic.JSUB)
            {
                address = 0;
                return false;
            }
            if (!instr.Operands[0].Value.HasValue)
            {
                address = 0;
                return false;
            }
            var disp = instr.Operands[0].Value.Value;
            if (!instr.Flags.HasValue)
            {
                // Maybe we should fail in this case?
                address = disp;
                return false;
            }
            var flags = instr.Flags.Value;
            if (    flags.HasFlag(Instruction.Flag.B)
                ||  flags.HasFlag(Instruction.Flag.X) 
                || (flags.HasFlag(Instruction.Flag.N) && !flags.HasFlag(Instruction.Flag.I)))
            {
                // Cannot predict the address because it depends on the value of B, X, or some other location in memory.
                address = 0;
                return false;
            }
            // Ignore I flag.
            //if (flags.HasFlag(Instruction.Flag.I) && !flags.HasFlag(Instruction.Flag.N))
            //{
            //    address = disp;
            //    return true;
            //}
            if (flags.HasFlag(Instruction.Flag.P))
            {
                if (!instr.Address.HasValue)
                {
                    address = 0;
                    return false;
                }
                bool dispPositive;
                if (flags.HasFlag(Instruction.Flag.E))
                {
                    disp = Instruction.Decode20BitTwosComplement(disp, out dispPositive);
                } else
                {
                    disp = Instruction.Decode12BitTwosComplement(disp, out dispPositive);
                }
                if (!dispPositive)
                    disp = -disp;
                address = disp + instr.Address.Value + (int)instr.Format;
                return true;
            }

            address = 0;
            return false;
        }

        public DisassemblyResult DisassembleReachable(Stream data, int start, int length)
        {
            data.Seek(start, SeekOrigin.Begin);
            var ret = new DisassemblyResult();
            int readingLeft = length;
            var starts = new LinkedList<int>();
            starts.AddFirst(start);
            bool continueAfterCurrent = true;
            do
            {
                var du = TryDisassembleInstruction(data, readingLeft);
                if (du != null)
                {
                    ret.AddInstruction(du);
                    readingLeft -= (int)du.Format;
                    if (Instruction.IsJump(du.Operation))
                    {
                        var jumpTarget = du.Operands[0].Value.Value;
                        var flags = du.Flags.Value;
                        if (flags.HasFlag(Instruction.Flag.P))
                        {
                            jumpTarget += (int)data.Position;
                        }
                        Debug.WriteLine($"Adding new start: {jumpTarget:x}");
                        starts.AddLast(jumpTarget);
                        if (du.Operation == Instruction.Mnemonic.J)
                        {
                            continueAfterCurrent = false;
                        }
                    }
                }
                if (!continueAfterCurrent)
                {
                    int newStart;
                    do
                    {
                        if (starts.Count == 0)
                        {
                            Debug.WriteLine("No more disassembly starts. Exiting.");
                            return ret;
                        }
                        newStart = starts.First.Value;
                        starts.RemoveFirst();
                        Debug.WriteLine($"Removed next start: {newStart:x}...");

                        // If instruction at 'newStart' has already been disassembled, discard this start and grab another.
                    }
                    while (ret.FindInstruction(newStart, out _));
                    Debug.WriteLine($"Using start: {newStart:x}...");
                    data.Seek(newStart, SeekOrigin.Begin);
                }

            } while (readingLeft > 0); // (starts.Count > 0);

            return ret;
        }

        //public DisassemblyResult Disassemble(byte[] data, int offset, int length)
        //{
        //    return Disassemble(new MemoryStream(data, offset, length), 0, length);
        //}

        /// <summary>
        /// Disassembles the data stream in the specified region, stopping if an invalid instruction is hit.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset">The index in the stream to begin reading.</param>
        /// <param name="length">The maximum number of bytes to read from the stream.</param>
        /// <returns></returns>
        public DisassemblyResult Disassemble(Stream data, int offset, int length)
        {
            data.Seek(offset, SeekOrigin.Begin);

            var ret = new DisassemblyResult();
            int successBytes = 0;
            Instruction nextInstr;
            while (successBytes < offset + length)
            {
                nextInstr = TryDisassembleInstruction(data, length);
                if (nextInstr == null)
                {
                    return ret;
                }

                ret.AddInstruction(nextInstr);
                successBytes += (int)nextInstr.Format;
            }
            return ret;
        }

        /// <summary>
        /// Tries to disassemble a single instruction beginning at the stream's current position. 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="readLimit"></param>
        /// <returns></returns>
        private Instruction TryDisassembleInstruction(Stream data, int readLimit)
        {
            if (readLimit < 1)
                return null; // Caller doesn't want us to read anymore.
            int firstByte = data.ReadByte();
            if (firstByte < 0)
                return null; // Reached end of stream.

            var opcode = Instruction.ParseMnemonic((byte)firstByte);
            if (!opcode.HasValue)
                return null; // Unrecognized opcode.

            var ret = new Instruction(opcode.Value);
            ret.Address = (int)data.Position - 1; // Minus 1 because we already read the first byte of it.

            if (ret.Format == InstructionFormat.Format1)
                return ret; // Format 1 instructions need no further processing.

            if (readLimit < 2)
                return null; // Caller doesn't want us to read anymore.
            int secondByte = data.ReadByte();
            if (secondByte < 0)
                return null; // Reached end of stream.

            if (ret.Format == InstructionFormat.Format2)
            {
                ret.Operands[0].Value = (secondByte & 0xf0) >> 4;
                if (ret.Operation != Instruction.Mnemonic.CLEAR) // CLEAR has only one operand.
                    ret.Operands[1].Value = secondByte & 0xf;
                return ret;
            }

            bool format4 = false;
            Instruction.Flag flags = 0;
            if ((firstByte & 0b10) != 0)
                flags |= Instruction.Flag.N;
            if ((firstByte & 1) != 0)
                flags |= Instruction.Flag.I;
            if ((secondByte & 0b10000000) != 0)
                flags |= Instruction.Flag.X;
            if ((secondByte & 0b01000000) != 0)
                flags |= Instruction.Flag.B;
            if ((secondByte & 0b00100000) != 0)
                flags |= Instruction.Flag.P;
            if ((secondByte & 0b00010000) != 0)
            {
                flags |= Instruction.Flag.E;
                format4 = true;
            }
            ret.Flags = flags;

            var debug_flagStrings = "";
            if (flags.HasFlag(Instruction.Flag.N))
                debug_flagStrings = "Indirect";
            if (flags.HasFlag(Instruction.Flag.I))
                debug_flagStrings += " Immediate";
            if (flags.HasFlag(Instruction.Flag.X))
                debug_flagStrings += " Indexed";
            if (flags.HasFlag(Instruction.Flag.B))
                debug_flagStrings += " Base";
            if (flags.HasFlag(Instruction.Flag.P))
                debug_flagStrings += " Program";
            if (flags.HasFlag(Instruction.Flag.E))
                debug_flagStrings += " Extended";

            if (ret.Operation == Instruction.Mnemonic.RSUB)
                return ret;

            // Instruction is format 3 or 4.
            if (readLimit < 3)
                return null; // Caller doesn't want us to read anymore.
            int thirdByte = data.ReadByte();
            if (thirdByte < 0)
                return null; // Reached end of stream.

            // Read a fourth byte if necessary.
            if (format4)
            {
                if (readLimit < 4)
                    return null; // Caller doesn't want us to read anymore.
                int fourthByte = data.ReadByte();
                if (fourthByte < 0)
                    return null; // Reached end of stream.

                ret.Operands[0].Value = (secondByte & 0x0f) << 16 | thirdByte << 8 | fourthByte;
                ret.Format = InstructionFormat.Format4;
                return ret;
            }
            else
            {
                //if (ret.Operation != Instruction.Mnemonic.RSUB) // RSUB has no operands.
                ret.Operands[0].Value = (secondByte & 0x0f) << 8 | thirdByte;
                ret.Format = InstructionFormat.Format3;
                return ret;
            }
        }

    }
}
