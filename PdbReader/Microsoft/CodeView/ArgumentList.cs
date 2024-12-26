using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class ArgumentList : ILeafRecord
    {
        internal _ArgumentList _header;
        internal List<uint> _arguments = new List<uint>();

        public LeafIndices LeafKind => LeafIndices.ArgumentList;

        internal static ArgumentList Create(PdbStreamReader reader, ref uint maxLength)
        {
            ArgumentList result = new ArgumentList();
            result._header = reader.Read<_ArgumentList>();
            for (int index = 0; index < result._header.count; index++) {
                result._arguments.Add(reader.ReadUInt32());
                Utils.SafeDecrement(ref maxLength, sizeof(uint));
            }
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _ArgumentList
        {
            internal ushort leaf; // LF_ARGLIST, LF_SUBSTR_LIST
            internal uint count; // number of arguments
        }
    }
}