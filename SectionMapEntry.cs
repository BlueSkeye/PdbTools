using System.Runtime.InteropServices;

namespace PdbReader
{
    public class SectionMapEntry
    {
        private const ushort NullStringOffset = ushort.MaxValue;
        private _SectionMapEntry _data;

        public string? ClassName { get; private set; }

        public SectionFlags Flags => _data.Flags;

        public string? SectionName { get; private set; }

        internal static SectionMapEntry Create(PdbStreamReader reader)
        {
            SectionMapEntry result = new SectionMapEntry() {
                _data = reader.Read<_SectionMapEntry>(),
            };
            ushort stringOffset = result._data.ClassName;
            result.ClassName = IsNullStringOffset(stringOffset)
                ? null
                : reader.Owner.GetPooledStringByOffset(stringOffset);
            stringOffset = result._data.SectionName;
            result.SectionName = IsNullStringOffset(stringOffset)
                ? null
                : reader.Owner.GetPooledStringByOffset(stringOffset);
            return result;
        }

        private static bool IsNullStringOffset(ushort candidate)
        {
            switch (candidate) {
                case ushort.MaxValue:
                case 0xEFFE:
                    return true;
                default:
                    return false;
            }
        }

        [Flags()]
        public enum SectionFlags : ushort
        {
            Read = 0x0001,
            Write = 0x0002,
            Execute = 0x0004,
            /// <summary>Descriptor describes a 32-bit linear address.</summary>
            AddressIs32Bit = 0x0008,
            /// <summary>Frame represents a selector.</summary>
            IsSelector = 0x0100,
            /// <summary>Frame represents an absolute address.</summary>
            IsAbsoluteAddress = 0x0200,
            /// <summary>If set, descriptor represents a group.</summary>
            IsGroup = 0x0400
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct _SectionMapEntry
        {
            internal SectionFlags Flags;
            // Logical overlay number
            internal ushort Ovl;
            // Group index into descriptor array.
            internal ushort Group;
            internal ushort Frame;
            // Byte index of segment / group name in string table, or 0xFFFF.
            internal ushort SectionName;
            // Byte index of class in string table, or 0xFFFF.
            internal ushort ClassName;
            // Byte offset of the logical segment within physical segment.
            // If group is set in flags, this is the offset of the group.
            internal uint Offset;
            // Byte count of the segment or group.
            internal uint SectionLength;
        }
    }
}
