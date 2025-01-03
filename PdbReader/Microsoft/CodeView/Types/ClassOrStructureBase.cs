using PdbReader.Microsoft.CodeView.Enumerations;
using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal abstract class ClassOrStructureBase : TypeRecord
    {
        internal _Class _class;
        // data describing length of structure in bytes and name
        internal ulong _structureSize;
        internal string _name;
        internal ulong _unknown;
        internal string? _decoratedName;

        protected ClassOrStructureBase(_Class @class, ulong structureSize, string name)
        {
            _class = @class;
            _structureSize = structureSize;
            _name = name;
            _unknown = 0;
            _decoratedName = null;
        }

        protected delegate ClassOrStructureBase InstanciatorDelegate(_Class header, ulong structureSize,
            string itemName);

        protected static ClassOrStructureBase Create(PdbStreamReader reader, ref uint maxLength,
            InstanciatorDelegate instanciator)
        {
            uint startOffset = reader.Offset;
            _Class header = reader.Read<_Class>();
            Utils.SafeDecrement(ref maxLength, _Class.Size);
            uint variantLength;
            ulong structureSize = (ulong)reader.ReadVariant(out variantLength);
            Utils.SafeDecrement(ref maxLength, variantLength);
            string itemName = reader.ReadNTBString(ref maxLength);
            ClassOrStructureBase result = instanciator(header, structureSize, itemName);
            // The unknown value is optional.
            if (sizeof(ushort) < maxLength)
            {
                // TODO : Understand why sometimes there is a single byte 0xF3
                // for example that can't strictly be considered padding.
                ulong unknown = (ulong)reader.ReadVariant(out variantLength);
                result._unknown = unknown;
                if (variantLength > maxLength)
                {
                    throw new BugException();
                }
                Utils.SafeDecrement(ref maxLength, variantLength);
                if (0 < maxLength)
                {
                    // We must expect an additional decorated name.
                    result._decoratedName = reader.ReadNTBString(ref maxLength);
                }
            }
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Class
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_Class>();
            internal TypeKind leaf; // LF_CLASS, LF_STRUCT, LF_INTERFACE
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field (prop_t)
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
            internal uint /*CV_typ_t*/ derived; // type index of derived from list if not zero
            internal uint /*CV_typ_t*/ vshape; // type index of vshape table for this class
        }
    }
}
