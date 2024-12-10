using System.Runtime.InteropServices;

namespace LibProvider.COFF
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_FILE_HEADER
    {
        [FieldOffset(0x00)]
        internal ushort Machine;
        [FieldOffset(0x02)]
        internal ushort NumberOfSections;
        [FieldOffset(0x4)]
        internal uint TimeDateStamp;
        [FieldOffset(0x08)]
        internal uint PointerToSymbolTable;
        [FieldOffset(0x0C)]
        internal uint NumberOfSymbols;
        [FieldOffset(0x10)]
        internal ushort SizeOfOptionalHeader;
        [FieldOffset(0x12)]
        internal ushort Characteristics;
    }
}
