using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Union : TypeRecord, INamedItem
    {
        internal _Union _data;
        // variable length data describing length of structure and name
        internal ulong _unionLength;
        internal string _name;
        internal string _decoratedName;

        public override LeafIndices LeafKind => LeafIndices.Union;

        public string Name => _name;

        internal static Union Create(PdbStreamReader reader, ref uint maxLength)
        {
            Union result = new Union();
            result._data = reader.Read<_Union>();
            Utils.SafeDecrement(ref maxLength, _Union.Size);
            uint variantLength;
            result._unionLength = (ulong)reader.ReadVariant(out variantLength);
            Utils.SafeDecrement(ref maxLength, variantLength);
            result._name = reader.ReadNTBString(ref maxLength);
            result._decoratedName = reader.ReadNTBString(ref maxLength);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Union
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_Union>();
            internal LeafIndices leaf; // LF_UNION
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
        }
    }
}
