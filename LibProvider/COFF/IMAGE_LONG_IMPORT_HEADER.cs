using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace LibProvider.COFF
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_LONG_IMPORT_HEADER
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

        internal IMAGE_LONG_IMPORT_HEADER(MemoryMappedViewStream from)
        {
            Machine = Utils.ReadLittleEndianUShort(from);
            NumberOfSections = Utils.ReadLittleEndianUShort(from);
            TimeDateStamp = Utils.ReadLittleEndianUInt32(from);
            PointerToSymbolTable = Utils.ReadLittleEndianUInt32(from);
            NumberOfSymbols = Utils.ReadLittleEndianUInt32(from);
            SizeOfOptionalHeader = Utils.ReadLittleEndianUShort(from);
            Characteristics = Utils.ReadLittleEndianUShort(from);
        }
    }
}
