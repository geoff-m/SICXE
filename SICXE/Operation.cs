
namespace SICXE
{
    enum Operation
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

        // I/O
        RD = 0xD8,
        TD = 0xE0,
        WD = 0xDC,
        STCH = 0x54,

        // Other
        COMP = 0x28,
        TIX = 0x2C
    }
}
