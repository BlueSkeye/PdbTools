﻿using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class FunctionIdentifier : TypeRecord
    {
        internal _FunctionIdentifier Identifier { get; private set; }
        internal string Name { get; private set; }

        internal static FunctionIdentifier Create(PdbStreamReader reader,
            ref uint maxLength)
        {
            FunctionIdentifier result = new FunctionIdentifier()
            {
                Identifier = reader.Read<_FunctionIdentifier>()
            };
            Utils.SafeDecrement(ref maxLength, _FunctionIdentifier.Size);
            result.Name = reader.ReadNTBString(ref maxLength);
            return result;
        }

        public override TypeKind LeafKind => TypeKind.FunctionIdentifier;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _FunctionIdentifier
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_FunctionIdentifier>();
            internal TypeKind leaf; // LF_FUNC_ID
            internal uint /*CV_ItemId*/ scopeId; // parent scope of the ID, 0 if global
            internal uint /*CV_typ_t*/ type; // function type
        }
    }
}
