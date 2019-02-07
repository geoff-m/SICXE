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
            while (data.Position < length)
            {
                nextInstr = TryDisassembleInstruction(data, length - offset);
                if (nextInstr != null)
                {
                    nextInstr.Address += offset; // Fix up offset.
                    ret.AddInstruction(nextInstr);
                }
            }
            return ret;
        }

        public DisassemblyResult Disassemble(byte[] data)
        {
            return Disassemble(new MemoryStream(data), 0, data.Length);
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
            while (successBytes < length)
            {
                nextInstr = TryDisassembleInstruction(data, length - offset);
                if (nextInstr == null)
                {
                    // tood fix up offset.
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

            // Instruction is format 3 or 4.
            if (readLimit < 3)
                return null; // Caller doesn't want us to read anymore.
            int thirdByte = data.ReadByte();
            if (thirdByte < 0)
                return null; // Reached end of stream.

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

            // Read a fourth byte if necessary.
            if (format4)
            {
                if (readLimit < 4)
                    return null; // Caller doesn't want us to read anymore.
                int fourthByte = data.ReadByte();
                if (fourthByte < 0)
                    return null; // Reached end of stream.

                ret.Operands[0].Value = (secondByte & 0x1f) << 16 | thirdByte << 8 | fourthByte;
                ret.Format = InstructionFormat.Format4;
                return ret;
            }
            else
            {
                if (ret.Operation != Instruction.Mnemonic.RSUB) // RSUB has no operands.
                    ret.Operands[0].Value = (secondByte & 0x1f) << 8 | thirdByte;
                ret.Format = InstructionFormat.Format3;
                return ret;
            }
        }

    }
}
