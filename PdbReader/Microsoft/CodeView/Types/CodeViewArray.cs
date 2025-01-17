﻿using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class CodeViewArray : TypeRecord, INamedItem
    {
        internal _Array _data;
        //variable length data specifying size in bytes and name
        internal ulong _arrayLength;
        internal string _name;

        public override TypeKind LeafKind => TypeKind.Array;

        public string Name => _name;

        private CodeViewArray(_Array data)
        {
            _data = data;
        }

        internal static CodeViewArray Create(PdbStreamReader reader, ref uint maxLength)
        {
            CodeViewArray result = new CodeViewArray(reader.Read<_Array>());
            Utils.SafeDecrement(ref maxLength, _Array.Size);
            uint variantSize;
            result._arrayLength = (ulong)reader.ReadVariant(out variantSize);
            Utils.SafeDecrement(ref maxLength, variantSize);
            result._name = reader.ReadNTBString(ref maxLength);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Array
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_Array>();
            internal TypeKind leaf; // LF_ARRAY
            internal uint /*CV_typ_t*/ elemtype; // type index of element type
            internal uint /*CV_typ_t*/ idxtype; // type index of indexing type
        }
    }
}
