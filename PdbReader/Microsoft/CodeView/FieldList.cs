using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class FieldList : TypeRecord
    {
        internal LeafIndices _leaf; // LF_FIELDLIST
        // char data[CV_ZEROLEN]; // field list sub lists
        internal List<INamedItem> _members = new List<INamedItem>();

        private FieldList(LeafIndices leaf)
        {
            _leaf = leaf;
        }

        public override LeafIndices LeafKind => LeafIndices.FieldList;

        internal static FieldList Create(IndexedStream stream, ref uint maxLength)
        {
            PdbStreamReader reader = stream._reader;
            uint endOffsetExcluded = maxLength + reader.Offset;
            FieldList result = new FieldList((LeafIndices)reader.ReadUInt16());
            Utils.SafeDecrement(ref maxLength, sizeof(ushort));
            while (0 < maxLength) {
                ITypeRecord memberRecord = stream.LoadTypeRecord(uint.MinValue, ref maxLength);
                result._members.Add((INamedItem)memberRecord);
            }
            if (endOffsetExcluded != reader.Offset) {
                throw new PDBFormatException("Field list end of record offset mismatch.");
            }
            return result;
        }
    }
}
