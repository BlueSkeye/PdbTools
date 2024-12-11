using System.Runtime.InteropServices;

namespace LibProvider.COFF
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_OPTIONAL_HEADER64
    {
        [FieldOffset(0x00)]
        internal ushort Magic;
        [FieldOffset(0x02)]
        internal byte MajorLinkerVersion;
        [FieldOffset(0x03)]
        internal byte MinorLinkerVersion;
        [FieldOffset(0x04)]
        internal uint SizeOfCode;
        [FieldOffset(0x08)]
        internal uint SizeOfInitializedData;
        [FieldOffset(0x0C)]
        internal uint SizeOfUninitializedData;
        [FieldOffset(0x10)]
        internal uint AddressOfEntryPoint;
        [FieldOffset(0x14)]
        internal uint BaseOfCode;
        // End of Optional header standard fields

        [FieldOffset(0x18)]
        internal IntPtr ImageBase;
        [FieldOffset(0x20)]
        internal uint SectionAlignment;
        [FieldOffset(0x24)]
        internal uint FileAlignment;
        [FieldOffset(0x28)]
        internal ushort MajorOperatingSystemVersion;
        [FieldOffset(0x2A)]
        internal ushort MinorOperatingSystemVersion;
        [FieldOffset(0x2C)]
        internal ushort MajorImageVersion;
        [FieldOffset(0x2E)]
        internal ushort MinorImageVersion;
        [FieldOffset(0x30)]
        internal ushort MajorSubsystemVersion;
        [FieldOffset(0x32)]
        internal ushort MinorSubsystemVersion;
        [FieldOffset(0x34)]
        internal uint Win32VersionValue;
        [FieldOffset(0x8)]
        internal uint SizeOfImage;
        [FieldOffset(0x3C)]
        internal uint SizeOfHeaders;
        [FieldOffset(0x40)]
        internal uint CheckSum;
        [FieldOffset(0x44)]
        internal ushort Subsystem;
        [FieldOffset(0x46)]
        internal ushort DllCharacteristics;
        [FieldOffset(0x48)]
        internal ulong SizeOfStackReserve;
        [FieldOffset(0x50)]
        internal ulong SizeOfStackCommit;
        [FieldOffset(0x58)]
        internal ulong SizeOfHeapReserve;
        [FieldOffset(0x60)]
        internal ulong SizeOfHeapCommit;
        [FieldOffset(0x68)]
        internal uint LoaderFlags;
        [FieldOffset(0x6C)]
        internal uint NumberOfRvaAndSizes;
        //[FieldOffset(0x70)]
        //IMAGE_DATA_DIRECTORY DataDirectory[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
    }
}
