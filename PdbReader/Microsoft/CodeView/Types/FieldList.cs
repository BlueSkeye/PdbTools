using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    //[StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class FieldList : TypeRecord
    {
        internal TypeKind _leaf; // LF_FIELDLIST
        // char data[CV_ZEROLEN]; // field list sub lists
        internal List<INamedItem> _members = new List<INamedItem>();

        private FieldList(TypeKind leaf)
        {
            _leaf = leaf;
        }

        public override TypeKind LeafKind => TypeKind.FieldList;

        internal static FieldList Create(TypeIndexedStream stream, ref uint maxLength)
        {
            PdbStreamReader reader = stream._reader;
            uint endOffsetExcluded = maxLength + reader.Offset;
            FieldList result = new FieldList((TypeKind)reader.ReadUInt16());
            Utils.SafeDecrement(ref maxLength, sizeof(ushort));
            while (0 < maxLength)
            {
                ITypeRecord memberRecord = stream.LoadTypeRecord(ref maxLength);
                result._members.Add((INamedItem)memberRecord);
            }
            if (endOffsetExcluded != reader.Offset)
            {
                throw new PDBFormatException("Field list end of record offset mismatch.");
            }
            return result;
        }
    }
}
