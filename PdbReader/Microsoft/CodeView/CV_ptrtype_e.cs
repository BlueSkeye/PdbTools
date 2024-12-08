
namespace PdbReader.Microsoft.CodeView
{
    internal enum CV_ptrtype_e : byte
    {
        Near = 0x00, // 16 bit pointer
        Far = 0x01, // 16:16 far pointer
        Huge = 0x02, // 16:16 huge pointer
        SegmentBased = 0x03, // based on segment
        ValueBased = 0x04, // based on value of base
        SegmentValueBased = 0x05, // based on segment value of base
        AddressBased = 0x06, // based on address of base
        SegmentAddressBased = 0x07, // based on segment address of base
        TypeBased = 0x08, // based on type
        SelfBased = 0x09, // based on self
        Near32 = 0x0a, // 32 bit pointer
        Far32 = 0x0b, // 16:32 pointer
        SixtyFourBitsPointer = 0x0c, // 64 bit pointer
    }
}
