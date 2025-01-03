using System.Runtime.InteropServices;
using static PdbReader.Microsoft.CodeView.Types.Index;

namespace PdbReader.Microsoft.CodeView.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class Label : TypeRecord
    {
        internal static readonly uint Size = (uint)Marshal.SizeOf<Label>();
        internal TypeKind leaf; // LF_LABEL
        internal CV_LABEL_TYPE_e mode; // addressing mode of label

        public override TypeKind LeafKind => TypeKind.Label;

        internal static Label Create(PdbStreamReader reader, ref uint maxLength)
        {
            Label result = new Label()
            {
                leaf = (TypeKind)reader.ReadUInt16(),
                mode = (CV_LABEL_TYPE_e)reader.ReadUInt16()
            };
            return result;
        }

        internal enum CV_LABEL_TYPE_e : ushort
        {
            CV_LABEL_NEAR = 0, // near return
            CV_LABEL_FAR = 4 // far return
        }
    }
}
