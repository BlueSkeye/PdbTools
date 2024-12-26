using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class VirtualTableShape : ILeafRecord
    {
        internal _VirtualTableShape _data;

        private VirtualTableShape(_VirtualTableShape data)
        {
            _data = data;
        }

        public LeafIndices LeafKind => LeafIndices.VirtualTableShape;

        internal static VirtualTableShape Create(PdbStreamReader reader, ref uint maxLength)
        {
            _VirtualTableShape data = reader.Read<_VirtualTableShape>();
            Utils.SafeDecrement(ref maxLength, _VirtualTableShape.Size);
            byte inputByte = 0;
            for(int index = 0; index < data.count; index++) {
                CV_VTS_desc_e entry = 0;
                if (0 == (index % 2)) {
                    inputByte = reader.ReadByte();
                    Utils.SafeDecrement(ref maxLength, sizeof(byte));
                    entry = (CV_VTS_desc_e)(inputByte & 0x0F);
                }
                else {
                    entry = (CV_VTS_desc_e)((inputByte & 0xF0) >> 4);
                }
                if (CV_VTS_desc_e.Unused == entry) {
                    throw new PDBFormatException("May be");
                }
            }
            // Some virtual table shapes appear to have padding bytes.
            Utils.SafeDecrement(ref maxLength, reader.HandlePadding(maxLength));
            return new VirtualTableShape(data);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _VirtualTableShape
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_VirtualTableShape>();
            internal LeafIndices leaf; // LF_VTSHAPE
            internal ushort count; // number of entries in vfunctable
            // unsigned char desc[CV_ZEROLEN];     // 4 bit (CV_VTS_desc) descriptors
        }
    }
}
