using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class BitField : TypeRecord
    {
        internal _BitField _data;

        private BitField(_BitField data)
        {
            _data = data;
        }

        public override TypeKind LeafKind => TypeKind.BitField;

        internal static BitField Create(PdbStreamReader reader, ref uint maxLength)
        {
            _BitField data = reader.Read<_BitField>();
            Utils.SafeDecrement(ref maxLength, _BitField.Size);
            // It looks like any BitField record is subject to padding.
            Utils.SafeDecrement(ref maxLength, reader.HandlePadding(maxLength));
            return new BitField(data);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _BitField
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_BitField>();
            internal TypeKind leaf; // LF_BITFIELD
            internal uint /*CV_typ_t*/ type; // type of bitfield
            internal byte length;
            internal byte position;
        }
    }
}
