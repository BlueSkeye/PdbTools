using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    /// <summary></summary>
    /// <remarks>Structures are byte aligned. SizeOf(PointerBody) = 10</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PointerBody
    {
        private static readonly uint PointerBodySize = (uint)Marshal.SizeOf<PointerBody>();

        internal LEAF_ENUM_e leaf; // LF_POINTER
        // type index of the underlying type
        internal uint utype;
        internal Attributes attr;

        internal static IPointer Create(PdbStreamReader reader, IndexedStream stream,
            uint recordLength)
        {
            if (PointerBodySize > recordLength) {
                throw new PDBFormatException("Invalid record length.");
            }
            uint startOffset = reader.Offset;
            PointerBody rawBody = reader.Read<PointerBody>();
            if (LEAF_ENUM_e.Pointer != rawBody.leaf) {
                throw new PDBFormatException(
                    $"Invalid leaf identifier {rawBody.leaf} found on pointer body.");
            }
            CV_ptrtype_e pointerType = rawBody.GetPointerType();
            switch (pointerType) {
                case CV_ptrtype_e.SegmentBased:
                    return SegmentBasedPointer.Create(reader, rawBody);
                case CV_ptrtype_e.TypeBased:
                    return TypeBasedPointer.Create(reader, rawBody);
                default:
                    switch (rawBody.GetPointerMode()) {
                        case CV_ptrmode_e.PointerToMember:
                        case CV_ptrmode_e.PointerToMemberFunction:
                            return PointerToMember.Create(reader, rawBody);
                        default:
                            uint remainingBytes = (uint)(recordLength - PointerBodySize);
                            return Pointer.Create(stream, reader, rawBody, remainingBytes);
                    }
            }
        }

        internal CV_ptrmode_e GetPointerMode()
        {
            CV_ptrmode_e result = (CV_ptrmode_e)(((ulong)attr >> 5) & 0x07);
            if (5 < (byte)result) {
                throw new PDBFormatException($"Unknown pointer mode found 0x{result:X2}");
            }
            return result;
        }

        /// <summary>Get pointer size in bytes.</summary>
        internal uint GetPointerSize() => (uint)((((ulong)attr) >> 13) & 0x3F);

        internal CV_ptrtype_e GetPointerType()
        {
            CV_ptrtype_e result = (CV_ptrtype_e)((ulong)attr & 0x1F);
            if (0x0C < (byte)result) {
                throw new PDBFormatException($"Unknown pointer type value 0x{((byte)result):X2}");
            }
            return result;
        }

        [Flags()]
        internal enum Attributes : uint
        {
            Flat32 = 0x00000100, // true if 0:32 pointer
            Volatile = 0x00000200, // TRUE if volatile pointer
            Constant = 0x00000400, // TRUE if const pointer
            Unaligned = 0x00000800, // TRUE if unaligned pointer
            Restricted = 0x00001000, // TRUE if restricted pointer (allow agressive opts)
            MoCOMPointer = 0x00080000, // TRUE if it is a MoCOM pointer (^ or %)
            LeftQualifier = 0x00100000, // TRUE if it is this pointer of member function with & ref-qualifier
            RightQualifier = 0x00200000, // TRUE if it is this pointer of member function with && ref-qualifier
        }
    }
}
