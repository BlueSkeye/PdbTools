using System.Runtime.InteropServices;

namespace PdbReader.TypeRecords
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ModifierRecord
    {
        internal readonly uint BaseTypeIndex;
        internal readonly _Flags Flags;
        internal readonly TypeRecordHeader Header;

        internal ModifierRecord(PdbStreamReader reader)
        {
            Header = reader.Read<TypeRecordHeader>();
            BaseTypeIndex = reader.ReadUInt32();
            Flags = (_Flags)reader.ReadUInt16();
        }

        [Flags()]
        internal enum _Flags : ushort
        {
            Constant = 0x0001,
            Volatile = 0x0002,
            Unaligned = 0x0004,
        }
    }
}
