using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Class
    {
        internal _Class _class;
        // data describing length of structure in bytes and name
        internal ulong _structureSize;
        internal string _name;
        internal ulong _unknown;
        internal string? _decoratedName;

        private Class(_Class @class, ulong structureSize, string name)
        {
            _class = @class;
            _structureSize = structureSize;
            _name = name;
            _unknown = 0;
            _decoratedName = null;
        }

        internal static Class Create(PdbStreamReader reader, uint recordLength)
        {
            uint startOffset = reader.Offset;
            _Class header = reader.Read<_Class>();
            ulong structureSize = reader.ReadVariableLengthValue();
            string itemName = reader.ReadNTBString();
            Class result = new Class(header, structureSize, itemName);
            uint remainingBytes = recordLength + startOffset;
            if (remainingBytes < reader.Offset) {
                throw new PDBFormatException("Record length mismatch.");
            }
            remainingBytes -= reader.Offset;
            // The unknown value is optional.
            if (sizeof(ushort) < remainingBytes) {
                // TODO : Understand why sometimes there is a single byte 0xF3
                // for example that can't strictly be considered padding.
                ulong unknown = reader.ReadVariableLengthValue();
                result._unknown = unknown;
                if ((reader.Offset - startOffset) < recordLength) {
                    // We must expect an additional decorated name.
                    result._decoratedName = reader.ReadNTBString();
                }
            }
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Class
        {
            internal LEAF_ENUM_e leaf; // LF_CLASS, LF_STRUCT, LF_INTERFACE
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field (prop_t)
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
            internal uint /*CV_typ_t*/ derived; // type index of derived from list if not zero
            internal uint /*CV_typ_t*/ vshape; // type index of vshape table for this class
        }
    }
}
