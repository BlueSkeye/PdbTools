using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace LibProvider.COFF
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_RELOCATION_ENTRY
    {
        [FieldOffset(0x00)]
        internal uint virtualAddress;
        [FieldOffset(0x04)]
        internal uint symbolIndex;
        [FieldOffset(0x08)]
        internal ushort relocationType;

        internal IMAGE_RELOCATION_ENTRY(MemoryMappedViewStream from)
        {
            virtualAddress = Utils.ReadLittleEndianUInt32(from);
            symbolIndex = Utils.ReadLittleEndianUInt32(from);
            relocationType = Utils.ReadLittleEndianUShort(from);
        }
    }
}
