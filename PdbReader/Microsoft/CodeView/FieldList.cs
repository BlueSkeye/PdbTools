using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FieldList : ILeafRecord
    {
        internal LeafIndices _leaf; // LF_FIELDLIST
        // char data[CV_ZEROLEN]; // field list sub lists
        internal List<INamedItem> _members = new List<INamedItem>();

        private FieldList(LeafIndices leaf)
        {
            _leaf = leaf;
        }

        public LeafIndices LeafKind => LeafIndices.FieldList;

        internal static FieldList Create(IndexedStream stream, ref uint maxLength)
        {
            PdbStreamReader reader = stream._reader;
            uint endOffsetExcluded = maxLength + reader.Offset;
            FieldList result = new FieldList((LeafIndices)reader.ReadUInt16());
            Utils.SafeDecrement(ref maxLength, sizeof(ushort));
            while (0 < maxLength) {
                ILeafRecord memberRecord = stream.LoadRecord(uint.MinValue, ref maxLength);
                result._members.Add((INamedItem)memberRecord);
            }
            if (endOffsetExcluded != reader.Offset) {
                throw new PDBFormatException("Field list end of record offset mismatch.");
            }
            return result;
        }
    }
}
