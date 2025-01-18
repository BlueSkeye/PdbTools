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

        /// <remarks>This is a special case. This method MUST handle type registration.</remarks>
        /// <summary></summary>
        /// <param name="stream"></param>
        /// <param name="recordIndex">At every time, this is the index of the next type record to be
        /// registered against the <see cref="Pdb"/> owning instance. This mean the value is adjusted by this
        /// method and the caller must account for this modification for further processing (hence the byref)
        /// parameter value.</param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        /// <exception cref="PDBFormatException"></exception>
        internal static FieldList Create(TypeIndexedStream stream, ref uint recordIndex, ref uint maxLength)
        {
            PdbStreamReader reader = stream._reader;
            Pdb owner = reader.Owner;
            uint endOffsetExcluded = maxLength + reader.Offset;
            FieldList result = new FieldList((TypeKind)reader.ReadUInt16());
            owner.RegisterType(recordIndex++, result);
            Utils.SafeDecrement(ref maxLength, sizeof(ushort));
            while (0 < maxLength) {
                ITypeRecord memberRecord = stream.LoadTypeRecord(ref recordIndex, ref maxLength);
                result._members.Add((INamedItem)memberRecord);
            }
            if (endOffsetExcluded != reader.Offset) {
                throw new PDBFormatException("Field list end of record offset mismatch.");
            }
            return result;
        }
    }
}
