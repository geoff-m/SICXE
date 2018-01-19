using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SICXE
{
    class Machine
    {
        public const int SIC_MEMORY_MAXIMUM = 0x8000; // 32K
        public const int SICXE_MEMORY_MAXIMUM = 0x100000; // 1 M

        public Machine(int memorySize = SICXE_MEMORY_MAXIMUM)
        {
            memory = new byte[memorySize];
        }

        public int ProgramCounter
        {
            get;
            private set;
        }

        private byte[] memory;

        Word regA, regL, regX, regB;

        /// <summary>
        /// Executes the given program on this machine.
        /// </summary>
        /// <param name="p">The SIC/XE program to be executed.</param>
        public void Execute(Program p)
        {
            foreach (Instruction inst in p)
            {
                Operand op1, op2;
                int addr;
                switch (inst.Mnemonic)
                {
                    case Mnemonic.LDA:
                        // Load 3 bytes into A.
                        op1 = inst.Operands[0];
                        addr = op1.Value.Value;
                        regA = ReadWord(addr, op1.AddressingMode);
                        break;
                    case Mnemonic.ADD:
                        // Add argument to A.
                        op1 = inst.Operands[0];
                        addr = op1.Value.Value;
                        regA += ReadWord(addr, op1.AddressingMode);
                        break;
                    case Mnemonic.STA:
                        // Store A in argument.
                        op1 = inst.Operands[0];
                        WriteWord(regA, op1.Value.Value, op1.AddressingMode);
                        break;
                }
            }
        }


        private Word ReadWord(int address, AddressingMode mode)
        {
            if (mode == AddressingMode.Immediate)
                return (Word)address;
            return Word.FromArray(memory, DecodeAddress(address, mode));
        }

        // Helper function for ReadWord and WriteWord.
        private int DecodeAddress(int address, AddressingMode mode)
        {
            switch (mode)
            {
                case AddressingMode.Immediate:
                    throw new ArgumentException("Addressing mode is immediate: address should not be decoded!");
                case AddressingMode.Simple: // todo: In Machine, replace this with Direct. "Simple" should be disallowed here.
                    return address;
                case AddressingMode.Indirect:
                    return address + (int)regX;
                case AddressingMode.RelativeToBase:
                    return address + (int)regB;
                case AddressingMode.RelativeToProgramCounter:
                    return address + ProgramCounter;
                default:
                    throw new ArgumentException("Illegal or unsupported addressing mode");
            }
        }

        private void WriteWord(Word w, int address, AddressingMode mode)
        {
            if (mode != AddressingMode.Immediate)
            {
                address = DecodeAddress(address, mode);
            }
            address *= 3; // to convert from word index to byte index.
            memory[address] = w.Low;
            memory[address + 1] = w.Middle;
            memory[address + 2] = w.High;
        }

        /// <summary>
        /// Writes a portion of this machine's memory to the console.
        /// </summary>
        /// <param name="startAddress">The address of the first word to print.</param>
        /// <param name="count">The number of words to print.</param>
        public void DumpWords(int startAddress, int count)
        {
            startAddress *= 3;
            count *= 3;
            int stop = startAddress + count;
            for (int wordIdx = startAddress; wordIdx < stop; wordIdx += 12)
            {
                Console.WriteLine("0x{0:X}: \t{1,5} {2,5} {3,5} {4,5}", wordIdx,
                    (int)Word.FromArray(memory, wordIdx),
                    (int)Word.FromArray(memory, wordIdx + 3),
                    (int)Word.FromArray(memory, wordIdx + 6),
                    (int)Word.FromArray(memory, wordIdx + 9));
            }
        }

        struct Word
        {
            public static Word FromArray(byte[] array, int start)
            {
                return new Word(array[start],
                    array[start + 1],
                    array[start + 2]);
            }

            public static explicit operator Word(int n)
            {
                return new Word((byte)n,
                                (byte)((n & 0xff00) >> 8),
                                (byte)((n & 0xff0000) >> 16));
            }

            public static explicit operator int(Word w)
            {
                return w.Low | w.Middle << 8 | w.High << 16;
            }

            public static Word operator +(Word x, Word y)
            {
                return (Word)((int)x + (int)y);
            }

            public Word(byte low, byte middle, byte high)
            {
                Low = low;
                Middle = middle;
                High = high;
            }
            public byte Low, Middle, High;

            public override string ToString()
            {
                return ((int)this).ToString();
            }
        }
    }
}
