
namespace SICXE
{
    enum AddressingMode
    {
        NotSet = 0,

        Simple = 1,
        Indirect = 2,
        Immediate = 3,
        Indexed = 4,
        RelativeToProgramCounter = 5,
        RelativeToBase = 6,
        Extended = 7
    }
}
