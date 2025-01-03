namespace PdbReader.Microsoft.CodeView.Enumerations
{
    [Flags()]
    internal enum CV_modifier_t : ushort
    {
        Constant = 0x0001,
        Volatile = 0x0002,
        Unaligned = 0x0004,
    }
}
