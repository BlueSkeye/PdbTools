using System.IO.MemoryMappedFiles;
using System.Text;

namespace LibProvider.COFF
{
    internal class IMAGE_SECTION_HEADER
    {
        // [FieldOffset(0x00)]
        // Actualy an IMAGE_SIZEOF_SHORT_NAME = 8 bytes array.
        internal ulong name;
        // File address
        // [FieldOffset(0x08)]
        internal uint physicalAddress;
        // Total size of the section when loaded into memory.
        // [FieldOffset(0x08)]
        internal uint virtualSize;

        /// <summary>The address of the first byte of the section when loaded
        /// into memory, relative to the image base. For object files, this is
        /// the address of the first byte before relocation is applied.</summary>
        // [FieldOffset(0x0C)]
        internal uint virtualAddress;
        /// <summary>The size of the initialized data on disk, in bytes. This
        /// value must be a multiple of the FileAlignment member of the
        /// IMAGE_OPTIONAL_HEADER structure. If this value is less than the
        /// VirtualSize member, the remainder of the section is filled with
        /// zeroes. If the section contains only uninitialized data, the member
        /// is zero.</summary>
        // [FieldOffset(0x10)]
        internal uint sizeOfRawData;
        /// <summary>A file pointer to the first page within the COFF file.
        /// This value must be a multiple of the FileAlignment member of the
        /// IMAGE_OPTIONAL_HEADER structure. If a section contains only
        /// uninitialized data, set this member is zero.</summary>
        // [FieldOffset(0x14)]
        internal uint pointerToRawData;
        /// <summary>A file pointer to the beginning of the relocation entries
        /// for the section. If there are no relocations, this value is zero.
        /// </summary>
        // [FieldOffset(0x18)]
        internal uint pointerToRelocations;
        /// <summary>A file pointer to the beginning of the line-number entries
        /// for the section. If there are no COFF line numbers, this value is
        /// zero.</summary>
        // [FieldOffset(0x1C)]
        internal uint pointerToLineNumbers;
        /// <summary>The number of relocation entries for the section. This
        /// value is zero for executable images.</summary>
        // [FieldOffset(0x20)]
        internal ushort numberOfRelocations;
        /// <summary>The number of line-number entries for the section.</summary>
        // [FieldOffset(0x22)]
        internal ushort numberOfLineNumbers;
        /// <summary>The characteristics of the image.</summary>
        // [FieldOffset(0x24)]
        internal Flags characteristics;

        internal IMAGE_SECTION_HEADER(MemoryMappedViewStream from)
        {
            FullName = ASCIIEncoding.ASCII.GetString(Utils.AllocateBufferAndAssertRead(from, 8))
                .Replace('\0', ' ')
                .Trim();
            int dollarIndex = FullName.IndexOf("$");
            if (-1 == dollarIndex) {
                CanonicalName = FullName;
                Suffix = string.Empty;
            }
            else {
                CanonicalName = FullName.Substring(0, dollarIndex);
                Suffix = FullName.Substring(dollarIndex + 1);
            }
            virtualSize = Utils.ReadLittleEndianUInt32(from);
            virtualAddress = Utils.ReadLittleEndianUInt32(from);
            sizeOfRawData = Utils.ReadLittleEndianUInt32(from);
            pointerToRawData = Utils.ReadLittleEndianUInt32(from);
            pointerToRelocations = Utils.ReadLittleEndianUInt32(from);
            pointerToLineNumbers = Utils.ReadLittleEndianUInt32(from);
            numberOfRelocations = Utils.ReadLittleEndianUShort(from);
            numberOfLineNumbers = Utils.ReadLittleEndianUShort(from);
            characteristics = (Flags)Utils.ReadLittleEndianUInt32(from);
        }

        internal string CanonicalName { get; private set; }

        internal string FullName { get; private set; }

        internal string Suffix { get; private set; }

        internal bool IsExecutable => (0 != (characteristics & Flags.Executable));

        [Flags()]
        internal enum Flags : uint
        {
            Reserved1 = 0x00000000,
            Reserved2 = 0x00000001,
            Reserved3 = 0x00000002,
            Reserved4 = 0x00000004,
            /// <summary>The section should not be padded to the next boundary.
            /// This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES.
            /// This is valid only for object files.</summary>
            NoPadding = 0x00000008,
            Reserved5 = 0x00000010,
            /// <summary>The section contains executable code.</summary>
            ContainsCode = 0x00000020,
            /// <summary>The section contains initialized data.</summary>
            InitializedData = 0x00000040,
            /// <summary>The section contains uninitialized data.</summary>
            UninitializedData = 0x00000080,
            /// <summary>Reserved for future use.</summary>
            LinkOther = 0x00000100,
            /// <summary>The section contains comments or other information.
            /// The .drectve section has this type. This is valid for object
            /// files only.</summary>
            LinkerInformation = 0x00000200,
            Reserved6 = 0x00000400,
            /// <summary>The section will not become part of the image. This is
            /// valid only for object files.</summary>
            LinkerRemove = 0x00000800,
            /// <summary>The section contains COMDAT data. For more information,
            /// see COMDAT Sections (Object Only). This is valid only for object
            /// files.</summary>
            LinkerCOMDAT = 0x00001000,
            /// <summary>The section contains data referenced through the global
            /// pointer (GP).</summary>
            GlobalPointerReferences = 0x00008000,
            /// <summary>Reserved for future use.</summary>
            Purgeable = 0x00020000,
            /// <summary>Reserved for future use.</summary>
            Memory16Bits = 0x00020000,
            /// <summary>Reserved for future use.</summary>
            Locked = 0x00040000,
            /// <summary>Reserved for future use.</summary>
            Preload = 0x00080000,
            /// <summary>Align data on a 1-byte boundary. Valid only for object
            /// files.</summary>
            Align1Byte = 0x00100000,
            /// <summary>Align data on a 2-byte boundary. Valid only for object
            /// files.</summary>
            Align2Bytes = 0x00200000,
            /// <summary>Align data on a 4-byte boundary. Valid only for object
            /// files.</summary>
            Align4Bytes = 0x00300000,
            /// <summary>Align data on a 8-byte boundary. Valid only for object
            /// files.</summary>
            Align8Bytes = 0x00400000,
            /// <summary>Align data on a 16-byte boundary. Valid only for object
            /// files.</summary>
            Align16Bytes = 0x00500000,
            /// <summary>Align data on a 32-byte boundary. Valid only for object
            /// files.</summary>
            Align32Bytes = 0x00600000,
            /// <summary>Align data on a 64-byte boundary. Valid only for object
            /// files.</summary>
            Align64Bytes = 0x00700000,
            /// <summary>Align data on a 128-byte boundary. Valid only for object
            /// files.</summary>
            Align128Bytes = 0x00800000,
            /// <summary>Align data on a 256-byte boundary. Valid only for object
            /// files.</summary>
            Align256Bytes = 0x00900000,
            /// <summary>Align data on a 512-byte boundary. Valid only for object
            /// files.</summary>
            Align512Bytes = 0x00A00000,
            /// <summary>Align data on a 1024-byte boundary. Valid only for object
            /// files.</summary>
            Align1024Bytes = 0x00B00000,
            /// <summary>Align data on a 2048-byte boundary. Valid only for object
            /// files.</summary>
            Align2048Bytes = 0x00C00000,
            /// <summary>Align data on a 4096-byte boundary. Valid only for object
            /// files.</summary>
            Align4096Bytes = 0x00D00000,
            /// <summary>Align data on a 8192-byte boundary. Valid only for object
            /// files.</summary>
            Align8192Bytes = 0x00E00000,
            /// <summary>The section contains extended relocations.</summary>
            ExtendedRelocations = 0x01000000,
            /// <summary>The section can be discarded as needed.</summary>
            Discardable = 0x02000000,
            /// <summary>The section cannot be cached.</summary>
            NonCacheable = 0x04000000,
            /// <summary>The section is not pageable.</summary>
            NonPageable = 0x08000000,
            /// <summary>The section can be shared in memory.</summary>
            Shared = 0x10000000,
            /// <summary>The section can be executed as code.</summary>
            Executable = 0x20000000,
            /// <summary>The section can be read.</summary>
            Readable = 0x40000000,
            /// <summary>The section can be written to.</summary>
            Writable = 0x80000000
        }
    }
}
