using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class VirtualTableShape
    {
        internal _VirtualTableShape _data;

        private VirtualTableShape(_VirtualTableShape data)
        {
            _data = data;
        }

        internal static VirtualTableShape Create(PdbStreamReader reader)
        {
            _VirtualTableShape data = reader.Read<_VirtualTableShape>();
            byte inputByte = 0;
            for(int index = 0; index < data.count; index++) {
                CV_VTS_desc_e entry = 0;
                if (0 == (index % 2)) {
                    inputByte = reader.ReadByte();
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
            reader.HandlePadding();
            return new VirtualTableShape(data);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _VirtualTableShape
        {
            internal LEAF_ENUM_e leaf; // LF_VTSHAPE
            internal ushort count; // number of entries in vfunctable
            // unsigned char desc[CV_ZEROLEN];     // 4 bit (CV_VTS_desc) descriptors
        }
    }
}
