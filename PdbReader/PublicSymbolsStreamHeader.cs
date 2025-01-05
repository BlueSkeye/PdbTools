using System.Runtime.InteropServices;

namespace PdbReader
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PublicSymbolsStreamHeader
    {
        internal uint SymHash;
        /// <summary>Total number of bytes used by the address map. Entry count could be computed by
        /// dividing by sizeof uint.</summary>
        internal uint AddressMapBytesCount;
        /// <summary>Total number of thunks entries.</summary>
        internal uint ThunksCount;
        internal uint SizeOfThunk;
        internal ushort ISectThunkTable;
        /// <summary>Unused : padding</summary>
        internal ushort Padding;
        internal uint OffThunkTable;
        /// <summary>Total number of section definitions in this stream.</summary>
        internal uint SectionsCount;
    }
}
