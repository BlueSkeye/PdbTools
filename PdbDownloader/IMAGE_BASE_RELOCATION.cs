using System.Runtime.InteropServices;

namespace PdbDownloader
{
    [StructLayout(LayoutKind.Explicit)]
    public class IMAGE_BASE_RELOCATION
    {
        [FieldOffset(0)]
        public uint VirtualAddress;
        [FieldOffset(4)]
        public uint SizeOfBlock;
    }
}
