using PdbReader.Microsoft.CodeView;
using PdbReader.Microsoft.CodeView.Symbols;

namespace PdbReader
{
    internal abstract partial class BaseSymbolStream : BaseStream
    {
        protected List<ISymbolRecord> _symbols = new List<ISymbolRecord>();
        protected readonly Dictionary<uint, ISymbolRecord> _symbolsByOffset =
            new Dictionary<uint, ISymbolRecord>();

        protected BaseSymbolStream(Pdb owner, ushort streamIndex)
            : base(owner, streamIndex)
        {
        }

        internal void LoadAllRecords()
        {
            uint startOffset = _reader.Offset;
            uint endOffsetExcluded = base.StreamSize;
            _symbols = new List<ISymbolRecord>();
            while (endOffsetExcluded > _reader.Offset) {
                uint symbolOffset = _reader.Offset;
                RegisterSymbol(symbolOffset, LoadSymbolRecord());
                _reader.EnsureAlignment(4);
            }
            if (endOffsetExcluded != _reader.Offset) {
                throw new PDBFormatException(
                    $"Current symbol stream offset 0x{_reader.Offset:X8} doesn't match expected end offset 0x{endOffsetExcluded:X8}.");
            }
            return;
        }

        protected ISymbolRecord LoadSymbolRecord()
        {
            ushort recordLength = _reader.ReadUInt16();
            uint readerStartOffset = _reader.Offset;
            // Most if not all definitions are from CVINFO.H
            SymbolKind symbolKind = (SymbolKind)_reader.ReadUInt16();
            try {
                switch (symbolKind) {
                    case SymbolKind.S_ANNOTATION:
                        return new ANNOTATION(_reader, recordLength, symbolKind);
                    case SymbolKind.S_ANNOTATIONREF:
                        return new ANNOTATIONREF(_reader, recordLength);
                    case SymbolKind.S_END:
                        return END.GetENDSymbolFor(_reader.Owner);
                    case SymbolKind.S_GPROC32:
                    case SymbolKind.S_GPROC32_ID:
                    case SymbolKind.S_LPROC32:
                    case SymbolKind.S_LPROC32_DPC:
                    case SymbolKind.S_LPROC32_DPC_ID:
                    case SymbolKind.S_LPROC32_ID:
                        return new PROCSYM32(_reader, recordLength, symbolKind);
                    case SymbolKind.S_LPROCREF:
                    case SymbolKind.S_PROCREF:
                        return new PROCREF(_reader, recordLength, symbolKind);
                    case SymbolKind.S_PUB32:
                        return new PUB32(_reader, recordLength);
                    case SymbolKind.S_SEPCODE:
                        return new SEPCODE(_reader, recordLength, symbolKind);
                    default:
                        // TODO : Account for padding pseudo bytes.
                        // Handling should match description from include file (i.e. should only
                        // appear in complex types).
                        string warningMessage = $"WARN : Unknwon symbol record kind '{symbolKind}' / 0x{((int)symbolKind):X4}";
                        Console.WriteLine(warningMessage);
                        throw new PDBFormatException(warningMessage);
                }
            }
            finally {
                _reader.EnsureAlignment(sizeof(uint));
                uint readerEndOffset = _reader.Offset;
                uint expectedEndOffset = readerStartOffset + recordLength; 
                if (expectedEndOffset != readerEndOffset) {
                    throw new PDBFormatException(
                        $"End offset 0x{readerEndOffset:X8} doesn't match expected end offset 0x{expectedEndOffset:X8}");
                }
            }
        }

        protected void RegisterSymbol(uint offset, ISymbolRecord record)
        {
            _symbols.Add(record);
            _symbolsByOffset.Add(offset, record);
            return;
        }
    }
}
