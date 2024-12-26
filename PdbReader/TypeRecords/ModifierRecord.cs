using System.Runtime.InteropServices;

namespace PdbReader.TypeRecords
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class ModifierRecord : TypeRecordHeader
    {
        internal readonly uint BaseTypeIndex;
        internal readonly _Flags Flags;

        internal ModifierRecord(PdbStreamReader reader)
            : base(reader)
        {
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
