using System.Runtime.InteropServices;

namespace PdbDownloader
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_DATA_DIRECTORY
    {
        internal const int IMAGE_NUMBEROF_DIRECTORY_ENTRIES = 0x10;

        /// <summary>The relative virtual address of this directory.</summary>
        [FieldOffset(0x00)]
        internal uint VirtualAddress;
        [FieldOffset(0x04)]
        internal uint Size;
    }
}
