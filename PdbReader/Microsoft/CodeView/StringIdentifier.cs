using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader.Microsoft.CodeView
{
    internal struct StringIdentifier
    {
        internal _StringIdentifier Identifier { get; private set; }
        internal string Name { get; private set; }

        internal static StringIdentifier Create(PdbStreamReader reader, ref uint maxLength)
        {
            _StringIdentifier identifier = reader.Read<_StringIdentifier>();
            Utils.SafeDecrement(ref maxLength, _StringIdentifier.Size);
            StringIdentifier result = new StringIdentifier() {
                Identifier = identifier,
                Name = reader.ReadNTBString(ref maxLength)
            };
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _StringIdentifier
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_StringIdentifier>();
            internal LEAF_ENUM_e leaf; // LF_STRING_ID
            internal uint /*CV_ItemId*/ id; // ID to list of sub string IDs
        }
    }
}
