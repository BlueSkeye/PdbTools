using System.Runtime.InteropServices;

namespace PdbReader.TypeRecords
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class PointerRecord : TypeRecordHeader
    {
        internal readonly _Attributes Attributes;
        internal readonly uint BaseTypeIndex;

        internal PointerRecord(PdbStreamReader reader)
            : base(reader)
        {
            BaseTypeIndex = reader.ReadUInt32();
            Attributes = (_Attributes)reader.ReadUInt32();
        }

        [Flags()]
        internal enum _Attributes : uint
        {
            /// <summary>A 16 bit pointer.</summary>
            Near16 = 0x00000000,
            /// <summary>A far 16:16 pointer.</summary>
            Far16 = 0x00000001,
            /// <summary>A huge 16:16 pointer</summary>
            Huge16 = 0x00000002,
            /// <summary>A segment based pointer.</summary>
            SegmentBase = 0x00000003,
            /// <summary>A value based pointer.</summary>
            ValueBased = 0x00000004,
            /// <summary>A segment and value based pointer.</summary>
            ValueAndSegmentBased = 0x00000005,
            /// <summary>An address based pointer.</summary>
            AddressBase = 0x00000006,
            /// <summary>A segment and address based pointer.</summary>
            AddressAndSegmentBased = 0x00000007,
            /// <summary>A type based pointer.</summary>
            TypeBased = 0x00000008,
            /// <summary>A self based pointer.</summary>
            SelfBased = 0x00000009,
            /// <summary>A 32 bit pointer.</summary>
            Near32 = 0x0000000A,
            /// <summary>A 16:32 pointer.</summary>
            Far32 = 0x0000000B,
            /// <summary>64 bit pointer.</summary>
            Near64 = 0x0000000C,

            KIND_MASK = 0x0000001F,

            /// <summary>A left value reference.</summary>
            LValueReference = 0x00000020,
            /// <summary>A pointer to data member.</summary>
            PointerToDataMember = 0x00000040,
            /// <summary>A pointer to function member.</summary>
            PointerToMemberFunction = 0x00000060,
            /// <summary>An rvalue reference.</summary>
            RValueReference = 0x00000080,

            MODE_MASK = 0x000000E0,

            /// <summary>A flat pointer.</summary>
            Flat32 = 0x00000100,
            /// <summary>A volatile pointer.</summary>
            Volatile = 0x00000200,
            /// <summary>A constant pointer.</summary>
            Const = 0x00000400, 
            /// <summary>An unaligned pointer.</summary>
            Unaligned = 0x00000800,
            /// <summary>A restricted pointer.</summary>
            Restrict = 0x00001000,
            /// <summary>A WinRT smart pointer.</summary>
            WinRTSmartPointer = 0x00080000,
            /// <summary>An lvalue this pointer to a member function.</summary>
            LValueRefThisPointer = 0x00100000,
            /// <summary>An rvalue this pointer to a member function.</summary>
            RValueRefThisPointer = 0x00200000,
        }
    }
}
