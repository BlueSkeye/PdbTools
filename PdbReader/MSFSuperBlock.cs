using System.Runtime.InteropServices;

namespace PdbReader
{
    /// <summary></summary>
    /// <remarks>https://llvm.org/docs/PDB/MsfFile.html#msf-superblock</remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MSFSuperBlock
    {
        // The magic value is the ASCII string "Microsoft C/C++ MSF 7.00\r\n"
        // followed by these 6 bytes : 1A 44 53 00 00 00
        internal ulong Magic1; // Expected value 0x666F736F7263694D
        internal ulong Magic2; // Expected value 0x202B2B432F432074
        internal ulong Magic3; // Expected value 0x30302E372046534D
        internal ulong Magic4; // Expected value 0x00000053441A0A0D
        /// <summary>The block size of the internal file system. Valid values are 512,
        /// 1024, 2048, and 4096 bytes. Certain aspects of the MSF file layout vary
        /// depending on the block sizes.</summary>
        internal uint BlockSize;
        /// <summary>The index of a block within the file, at which begins a bitfield
        /// representing the set of all blocks within the file which are “free” (i.e.
        /// the data within that block is not used). See The Free Block Map for more
        /// information. Important: FreeBlockMapBlock can only be 1 or 2!</summary>
        internal uint FreeBlockMapBlock;
        /// <summary>The total number of blocks in the file. NumBlocks * BlockSize
        /// should equal the size of the file on disk.</summary>
        internal uint NumBlocks;
        /// <summary>The size of the stream directory, in bytes. The stream directory
        /// contains information about each stream’s size and the set of blocks that
        /// it occupies. It will be described in more detail later.</summary>
        internal uint NumDirectoryBytes;
        internal uint Unknown;
        /// <summary>The index of a block within the MSF file. At this block is an
        /// array of uint’s listing the blocks that the stream directory resides on.
        /// For large MSF files, the stream directory (which describes the block layout
        /// of each stream) may not fit entirely on a single block. As a result, this
        /// extra layer of indirection is introduced, whereby this block contains the
        /// list of blocks that the stream directory occupies, and the stream directory
        /// itself can be stitched together accordingly. The number of uint’s in this
        /// array is given by ceil(NumDirectoryBytes / BlockSize).</summary>
        internal uint BlockMapAddr;

        internal PdbKind AssertSignatureType(bool throwOnUnknown = true)
        {
            if (   (0x666F736F7263694D == Magic1)
                && (0x202B2B432F432074 == Magic2)
                && (0x30302E372046534D == Magic3)
                && (0x00000053441A0A0D == Magic4))
            {
                return PdbKind.Regular;
            }
            if (0x424A5342 == (uint)Magic1) {
                return PdbKind.DotNet;
            }
            if (!throwOnUnknown) {
                return PdbKind.Undefined;
            }
            throw new PDBFormatException("Invalid signature.");
        }

        internal enum PdbKind
        {
            Undefined = 0x00,
            Regular = 0x01,
            DotNet = 0x02
        }
    }
}
