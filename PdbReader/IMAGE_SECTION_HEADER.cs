using System.Runtime.InteropServices;

namespace PdbReader.Microsoft
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IMAGE_SECTION_HEADER
    {
        public byte Name0;
        public byte Name1;
        public byte Name2;
        public byte Name3;
        public byte Name4;
        public byte Name5;
        public byte Name6;
        public byte Name7;
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SIzeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLineNumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLineNumbers;
        public _Characteristics Characteristics;

        [Flags()]
        public enum _Characteristics : uint
        {
            /// <summary>The section should not be padded to the next boundary.
            /// This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES.
            /// This is valid only for object files.</summary>
            NoPadding = 0x00000008,
            /// <summary>The section contains executable code.</summary>
            ContainsCode = 0x00000020,
            /// <summary>The section contains initialized data.</summary>
            InitializedData = 0x00000040,
            /// <summary>The section contains uninitialized data.</summary>
            UninitializedData = 0x00000080,
            /// <summary>The section contains comments or other information.
            /// The.drectve section has this type. This is valid for object files only.</summary>
            LinkerInfo = 0x00000200,
            /// <summary>The section will not become part of the image. This is valid
            /// only for object files.</summary>
            LinkerShouldRemove = 0x00000800,
            /// <summary>The section contains COMDAT data. For more information, see COMDAT
            /// Sections (Object Only). This is valid only for object files.</summary>
            COMDATData = 0x00001000,
            /// <summary>The section contains data referenced through the global pointer (GP).</summary>
            GlobalPointerReferencedData = 0x00008000,
            /// <summary>Align data on a 1-byte boundary. Valid only for object files.</summary>
            AlignTo1Byte = 0x00100000,
            /// <summary>Align data on a 2-byte boundary. Valid only for object files.</summary>
            AlignTo2Bytes = 0x00200000,
            /// <summary>Align data on a 4-byte boundary. Valid only for object files.</summary>
            AlignTo4Bytes = 0x00300000,
            /// <summary>Align data on a 8-byte boundary. Valid only for object files.</summary>
            AlignTo8Bytes = 0x00400000,
            /// <summary>Align data on a 16-byte boundary. Valid only for object files.</summary>
            AlignTo16Bytes = 0x00500000,
            /// <summary>Align data on a 32-byte boundary. Valid only for object files.</summary>
            AlignTo32Bytes = 0x00600000,
            /// <summary>Align data on a 64-byte boundary. Valid only for object files.</summary>
            AlignTo64Bytes = 0x00700000,
            /// <summary>Align data on a 128-byte boundary. Valid only for object files.</summary>
            AlignTo128Bytes = 0x00800000,
            /// <summary>Align data on a 256-byte boundary. Valid only for object files.</summary>
            AlignTo256Bytes = 0x00900000,
            /// <summary>Align data on a 512-byte boundary. Valid only for object files.</summary>
            AlignTo512Bytes = 0x00A00000,
            /// <summary>Align data on a 1024-byte boundary. Valid only for object files.</summary>
            AlignTo1024Bytes = 0x00B00000,
            /// <summary>Align data on a 2048-byte boundary. Valid only for object files.</summary>
            AlignTo2048Bytes = 0x00C00000,
            /// <summary>Align data on a 4096-byte boundary. Valid only for object files.</summary>
            AlignTo4096Bytes = 0x00D00000,
            /// <summary>Align data on a 8192-byte boundary. Valid only for object files.</summary>
            AlignTo8192Bytes = 0x00E00000,
            /// <summary>The section contains extended relocations.</summary>
            HasExtendedRelocations = 0x01000000,
            /// <summary>The section can be discarded as needed.</summary>
            Discardable = 0x02000000,
            /// <summary>The section cannot be cached.</summary>
            NotCacheable = 0x04000000,
            /// <summary>The section is not pageable.</summary>
            NotPageable = 0x08000000,
            /// <summary>The section can be shared in memory.</summary>
            Shareable = 0x10000000,
            /// <summary>The section can be executed as code.</summary>
            Executable = 0x20000000,
            /// <summary>The section can be read.</summary>
            Readable = 0x40000000,
            /// <summary>The section can be written to.</summary>
            Writable = 0x80000000
        }
    }
}
