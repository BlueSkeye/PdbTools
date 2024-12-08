
namespace PdbReader.Microsoft.CodeView
{
    /// <remarks>The value looks like it can be encoded on 3 bits.</remarks>
    internal enum CV_VTS_desc_e : byte
    {
        Near = 0x00,
        Far = 0x01,
        Thin = 0x02,
        Outer = 0x03,
        Meta = 0x04,
        Near32 = 0x05,
        Far32 = 0x06,
        Unused = 0x07
    }
}
