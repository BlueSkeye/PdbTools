
namespace PdbReader
{
    /// <summary>The Module Info Stream (henceforth referred to as the Modi stream) contains information
    /// about a single module (object file, import library, etc that contributes to the binary this PDB
    /// contains debug information about. There is one modi stream for each module, and the mapping between
    /// modi stream index and module is contained in the DBI Stream. The modi stream for a single module
    /// contains line information for the compiland, as well as all CodeView information for the symbols
    /// defined in the compiland. Finally, there is a “global refs” substream which is not well understood.
    /// </summary>
    /// <remarks>See https://llvm.org/docs/PDB/ModiStream.html</remarks>
    internal class ModuleInformationStream : IndexedStream
    {
        private readonly string _streamName;
        private readonly uint[] _symbols;

        internal ModuleInformationStream(Pdb owner, ushort index, uint symbolSize, uint c11LinesCount,
            uint c13LinesCount)
            : base(owner, index)
        {
            _streamName = $"Modi#{index}";
            uint signatures = _reader.ReadUInt32();
            if (4 != signatures) {
                throw new PDBFormatException($"Unsupported signature format : {signatures}");
            }
            if (0 != c11LinesCount) {
                throw new NotSupportedException("Some C11 line code information found. Format is unknown.");
            }
            int symbolsCount = Utils.SafeCastToInt32(symbolSize - sizeof(uint));
            _symbols = new uint[symbolsCount];
            for(int symbolIndex = 0; symbolIndex < symbolsCount; symbolIndex++) {
                _symbols[symbolIndex] = _reader.ReadUInt32();
            }
            uint globalReferencesSize = _reader.ReadUInt32();
            _reader.Offset += globalReferencesSize;
        }

        internal override string StreamName => _streamName;
    }
}
