﻿using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Enumerate : INamedItem, ILeafRecord
    {
        internal _Enumerate _header;
        // variable length value field followed
        // by length prefixed name of field
        internal ulong _enumerationValue;
        internal string _name;

        public LeafIndices LeafKind => LeafIndices.Enumerate;

        public string Name => _name;

        internal static Enumerate Create(PdbStreamReader reader, ref uint maxLength)
        {
            Enumerate result = new Enumerate();
            result._header = reader.Read<_Enumerate>();
            Utils.SafeDecrement(ref maxLength, _Enumerate.Size);
            uint variantSize;
            result._enumerationValue = (ulong)reader.ReadVariant(out variantSize);
            Utils.SafeDecrement(ref maxLength, variantSize);
            result._name = reader.ReadNTBString(ref maxLength);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Enumerate
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_Enumerate>();
            internal LeafIndices _leaf; // LF_ENUMERATE
            internal CV_fldattr_t attr; // attribute mask
        }
    }
}
