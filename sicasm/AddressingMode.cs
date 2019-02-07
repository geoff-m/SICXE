
namespace SICXEAssembler
{
    public enum AddressingMode
    {
        NotSet = 0,

        Simple = 1,
        Indirect = 2,
        Immediate = 4,
        Indexed = 8,
        RelativeToProgramCounter = 16,
        RelativeToBase = 32,
        Extended = 64
    }
}
