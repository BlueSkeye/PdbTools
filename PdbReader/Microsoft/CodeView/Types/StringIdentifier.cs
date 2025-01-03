using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class StringIdentifier : TypeRecord
    {
        internal _StringIdentifier Identifier { get; private set; }

        public override TypeKind LeafKind => TypeKind.StringIdentifier;

        internal string Name { get; private set; }

        internal static StringIdentifier Create(PdbStreamReader reader, ref uint maxLength)
        {
            _StringIdentifier identifier = reader.Read<_StringIdentifier>();
            Utils.SafeDecrement(ref maxLength, _StringIdentifier.Size);
            StringIdentifier result = new StringIdentifier()
            {
                Identifier = identifier,
                Name = reader.ReadNTBString(ref maxLength)
            };
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _StringIdentifier
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_StringIdentifier>();
            internal TypeKind leaf; // LF_STRING_ID
            internal uint /*CV_ItemId*/ id; // ID to list of sub string IDs
        }
    }
}
