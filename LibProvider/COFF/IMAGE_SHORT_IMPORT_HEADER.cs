using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace LibProvider.COFF
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_SHORT_IMPORT_HEADER
    {
        [FieldOffset(0x00)]
        internal ushort Sig1;
        [FieldOffset(0x02)]
        internal ushort Sig2;
        [FieldOffset(0x04)]
        internal ushort Version;
        [FieldOffset(0x06)]
        internal ushort Machine;
        [FieldOffset(0x8)]
        internal uint TimeDateStamp;
        [FieldOffset(0xC)]
        internal uint SizeOfData;
        [FieldOffset(0x10)]
        internal ushort OrdinalHint;
        [FieldOffset(0x12)]
        internal TypeKind Type;

        internal IMAGE_SHORT_IMPORT_HEADER(MemoryMappedViewStream from)
        {
            Sig1 = Utils.ReadLittleEndianUShort(from);
            if (0 != Sig1) {
                throw new ParsingException($"Unexpected Sig1 value 0x{Sig1:X4}. 0x0000 was expected.");
            }
            Sig2 = Utils.ReadLittleEndianUShort(from);
            if (0xFFFF != Sig2) {
                throw new ParsingException($"Unexpected Sig2 value 0x{Sig1:X4}. 0xFFFF was expected.");
            }
            Version = Utils.ReadLittleEndianUShort(from);
            Machine = Utils.ReadLittleEndianUShort(from);
            TimeDateStamp = Utils.ReadLittleEndianUInt32(from);
            SizeOfData = Utils.ReadLittleEndianUInt32(from);
            OrdinalHint = Utils.ReadLittleEndianUShort(from);
            Type = (TypeKind)Utils.ReadLittleEndianUShort(from);
        }

        [Flags()]
        internal enum TypeKind : ushort
        {
            TYPE_MASK = 0x0003,
            CodeImport = 0x0000,
            DataImport = 0x0001,
            ConstantImport = 0x0002,

            NAME_TYPE_MASK = 0x000C,
            /// <summary>The import is by ordinal. This indicates that the value in the Ordinal/Hint field
            /// of the import header is the import's ordinal. If this constant is not specified, then the
            /// Ordinal/Hint field should always be interpreted as the import's hint.</summary>
            OrdinalImport = 0x0000,
            /// <summary>The import name is identical to the public symbol name.</summary>
            NameImport = 0x0004,
            /// <summary>The import name is the public symbol name, but skipping the leading ?, @, or
            /// optionally _.</summary>
            NoPrefixNameImport = 0x0008,
            /// <summary>The import name is the public symbol name, but skipping the leading ?, @, or
            /// optionally _, and truncating at the first @.</summary>
            UndecoratedNameImport = 0x000C

        }
    }
}
