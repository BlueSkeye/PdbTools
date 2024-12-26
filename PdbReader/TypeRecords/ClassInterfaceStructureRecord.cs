using System.Runtime.InteropServices;

namespace PdbReader.TypeRecords
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class ClassInterfaceStructureRecord : TypeRecordHeader
    {
        internal ClassInterfaceStructureRecord(PdbStreamReader reader)
            :base(reader)
        {
            ushort membersCount = reader.ReadUInt16();
            Attributes = (_Attributes)reader.ReadUInt16();
            uint fieldIndex = reader.ReadUInt32();
            BaseTypeIndex = reader.ReadUInt32();
            uint vtableShapeIndex = reader.ReadUInt32();
            // TODO : Unclear how to read this field. AsmResolver seems to consider this a LeafKind tag
            // followed by the value itself. For time now we read a ushort.
            Size = reader.ReadUInt16();
            Name = reader.ReadNTBString();
            if (!HasUniqueName) {
                int i = 1;
            }
            UniqueName = reader.ReadNTBString();
            return;
        }

        internal _Attributes Attributes { get; private set; }

        internal uint BaseTypeIndex { get; private set; }

        internal bool HasUniqueName => (0 != (_Attributes.HasUniqueName & Attributes));

        internal string Name { get; private set; }

        internal ushort Size { get; private set; }

        internal string UniqueName { get; private set; }

        [Flags()]
        internal enum _Attributes : ushort
        {
            /// <summary>Packed structure.</summary>
            Packed = 0x0001,
            /// <summary>A constructor or destructor.</summary>
            Constructor = 0x0002,
            /// <summary>An overloaded operator.</summary>
            OverloadedOperator = 0x0004,
            /// <summary>A nested class.</summary>
            IsNested = 0x0008,
            /// <summary>Nested types.</summary>
            CNested = 0x0010,
            /// <summary>Assigment operator.</summary>
            AssignmentOPerator = 0x0020,
            /// <summary>Casting method.</summary>
            CastingOperator = 0x0040,
            /// <summary>A forward reference.</summary>
            ForwardReference = 0x0080,
            /// <summary>A scoped definition.</summary>
            Scoped = 0x0100,
            /// <summary>A decorated name follows the regular name.</summary>
            HasUniqueName = 0x0200,
            /// <summary>A sealed class. Can't be overriden.</summary>
            Sealed = 0x0400,

            /// <summary>
            /// Defines the mask for the floating point type that is used within this structure.
            /// </summary>
            HFA_MASK = 0x1800,

            /// <summary>An intrinsic type.</summary>
            Intrinsic = 0x2000,
            COM_MASK = 0x3000,
        }
    }
}
