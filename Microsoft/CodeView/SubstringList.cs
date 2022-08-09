using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class SubstringList
    {
        internal _SubstringList _header;
        internal List<uint> _subStrings = new List<uint>();

        internal static SubstringList Create(PdbStreamReader reader)
        {
            SubstringList result = new SubstringList() {
                _header = reader.Read<_SubstringList>()
            };
            for (int index = 0; index < result._header.count; index++) {
                result._subStrings.Add(reader.ReadUInt32());
            }
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _SubstringList
        {
            internal ushort leaf; // LF_SUBSTR_LIST
            internal uint count; // number of substrings
        }
    }
}
