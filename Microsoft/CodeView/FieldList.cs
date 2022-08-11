using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FieldList
    {
        internal LEAF_ENUM_e _leaf; // LF_FIELDLIST
        // char data[CV_ZEROLEN]; // field list sub lists
        internal List<INamedItem> _members = new List<INamedItem>();

        private FieldList(LEAF_ENUM_e leaf)
        {
            _leaf = leaf;
        }
        
        internal static FieldList Create(IndexedStream stream, ref uint maxLength)
        {
            PdbStreamReader reader = stream._reader;
            uint endOffsetExcluded = maxLength + reader.Offset;
            FieldList result = new FieldList((LEAF_ENUM_e)reader.ReadUInt16());
            Utils.SafeDecrement(ref maxLength, sizeof(ushort));
            while (0 < maxLength) {
                LEAF_ENUM_e recordKind;
                object memberRecord = stream.LoadRecord(uint.MinValue, ref maxLength,
                    out recordKind);
                result._members.Add((INamedItem)memberRecord);
            }
            if (endOffsetExcluded != reader.Offset) {
                throw new PDBFormatException("Field list end of record offset mismatch.");
            }
            return result;
        }
    }
}
