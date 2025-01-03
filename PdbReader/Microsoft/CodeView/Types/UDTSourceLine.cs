using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class UDTSourceLine : TypeRecord
    {
        internal TypeKind leaf; // LF_UDT_SRC_LINE
        internal uint /*CV_typ_t*/ type; // UDT's type index
        internal uint /*CV_ItemId*/ src; // index to LF_STRING_ID record where source file name is saved
        internal uint line; // line number

        public override TypeKind LeafKind => TypeKind.UDTSourceLine;

        internal static UDTSourceLine Create(PdbStreamReader reader, ref uint maxLength)
        {
            UDTSourceLine result = new UDTSourceLine()
            {
                leaf = (TypeKind)reader.ReadUInt16(),
                type = reader.ReadUInt32(),
                src = reader.ReadUInt32(),
                line = reader.ReadUInt32()
            };
            return result;
        }
    }
}
