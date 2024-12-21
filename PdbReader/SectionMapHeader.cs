using System.Runtime.InteropServices;

namespace PdbReader
{
    /// <remarks>See https://llvm.org/docs/PDB/DbiStream.html#id6</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SectionMapHeader
    {
        // Number of segment descriptors in table
        internal ushort SecCount;
        // Number of logical segment descriptors
        internal ushort SecCountLog;
    }
}
