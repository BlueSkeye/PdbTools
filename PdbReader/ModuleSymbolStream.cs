﻿using PdbReader.Microsoft.CodeView;

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
    internal class ModuleSymbolStream : BaseSymbolStream
    {
        private readonly string _streamName;

        /// <summary></summary>
        /// <param name="owner"></param>
        /// <param name="index"></param>
        /// <param name="symbolSize"></param>
        /// <param name="c11BytesSize"></param>
        /// <param name="c13BytesSize"></param>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="PDBFormatException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        internal ModuleSymbolStream(Pdb owner, ushort index, uint symbolSize, uint c11BytesSize,
            uint c13BytesSize)
            : base(owner, index)
        {
            _streamName = $"Modi#{index}";
            if (0 != c11BytesSize) {
                throw new NotSupportedException("Some C11 line code information found. Format is unknown.");
            }
            int symbolsCount = Utils.SafeCastToInt32(symbolSize - sizeof(Signature));
            uint startOffset = _reader.Offset;
            uint endOffsetExcluded = startOffset + symbolSize;

            Signature signature = (Signature)_reader.ReadUInt32();
            if (Signature.C13 != signature) {
                throw new PDBFormatException($"Unsupported signature format : {signature}");
            }
            while (endOffsetExcluded > _reader.Offset) {
                uint symbolOffset = _reader.Offset;
                ISymbolRecord newRecord = base.LoadSymbolRecord();
                base.RegisterSymbol(symbolOffset, newRecord);
                _reader.EnsureAlignment(sizeof(uint));
            }
            if (endOffsetExcluded != _reader.Offset) {
                throw new PDBFormatException($"Invalid end offset.");
            }
            Console.WriteLine($" [*] {_symbols.Count} symbols found in {_streamName} : ");
            return;
        }

        internal override string StreamName => _streamName;

        internal ISymbolRecord GetSymbolByOffset(uint offset)
        {
            ISymbolRecord? result;
            if (!_symbolsByOffset.TryGetValue(offset, out result)) {
                throw new PDBFormatException(
                    $"No symbol found at offset 0x{offset:X8} in module {_streamName} (idx = {StreamIndex})");
            }
            return result;
        }

        internal enum Signature : uint
        {
            C6 = 0, // Actual signature is >64K
            C7 = 1, // First explicit signature
            C11 = 2, // C11 (vc5.x) 32-bit types
            C13 = 4, // C13 (vc7.x) zero terminated names        }
        }
    }
}
