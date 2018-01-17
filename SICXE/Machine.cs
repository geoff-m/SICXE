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
                switch (inst.Operation)
                {
                    case Operation.LDA:
                        // Load 3 bytes into A.
                        regA = ReadWord(inst.Argument1, inst.AddressingMode);
                        break;
                    case Operation.ADD:
                        // Add argument to A.
                        regA += ReadWord(inst.Argument1, inst.AddressingMode);
                        break;
                    case Operation.STA:
                        // Store A in argument.
                        WriteWord(regA, inst.Argument1, inst.AddressingMode);
                        break;
                }
            }
        }

        private int DecodeAddress(int address, AddressingMode mode)
        {
            switch (mode)
            {
                case AddressingMode.Immediate:
                    throw new ArgumentException("Addressing mode is immediate: address should not be decoded!");
                case AddressingMode.Direct:
                    return address;
                case AddressingMode.Indirect:
                    return address + (int)regX;
                case AddressingMode.RelativeToBase:
                    return address + (int)regB;
                case AddressingMode.RelativeToPC:
                    return address + ProgramCounter;
                default:
                    throw new ArgumentException("Illegal or unsupported addressing mode");
            }
        }

        private Word ReadWord(int address, AddressingMode mode)
        {
            if (mode == AddressingMode.Immediate)
                return (Word)address;
            return Word.FromArray(memory, DecodeAddress(address, mode));
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

        public void DumpWords(int startAddress, int count)
        {
            int stop = startAddress + count;
            for (int i = startAddress; i < stop; ++i)
            {
                Console.WriteLine("{0:x} {1}")
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
