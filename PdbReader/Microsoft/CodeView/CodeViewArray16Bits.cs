using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class CodeViewArray16Bits : INamedItem
    {
        internal _Array16Bits _data;
        //variable length data specifying size in bytes and name
        internal ulong _arrayLength;
        internal string _name;

        public string Name => _name;

        private CodeViewArray16Bits(_Array16Bits data)
        {
            _data = data;
        }

        internal static CodeViewArray16Bits Create(PdbStreamReader reader,
            ref uint maxLength)
        {
            CodeViewArray16Bits result = new CodeViewArray16Bits(reader.Read<_Array16Bits>());
            Utils.SafeDecrement(ref maxLength, _Array16Bits.Size);
            uint variantSize;
            result._arrayLength = (ulong)reader.ReadVariant(out variantSize);
            Utils.SafeDecrement(ref maxLength, variantSize);
            result._name = reader.ReadNTBString(ref maxLength);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Array16Bits
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_Array16Bits>();
            internal LEAF_ENUM_e leaf; // LF_ARRAY_16t
            internal ushort /*CV_typ16_t*/ elemtype; // type index of element type
            internal ushort /*CV_typ16_t*/ idxtype; // type index of indexing type
        }
    }
}
