using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Label
    {
        internal static readonly uint Size = (uint)Marshal.SizeOf<Label>();
        internal LEAF_ENUM_e leaf; // LF_LABEL
        internal CV_LABEL_TYPE_e mode; // addressing mode of label

        internal static Label Create(PdbStreamReader reader)
        {
            Label result = reader.Read<Label>();
            return result;
        }
        
        internal enum CV_LABEL_TYPE_e : ushort
        {
            CV_LABEL_NEAR = 0, // near return
            CV_LABEL_FAR = 4 // far return
        }
    }
}
