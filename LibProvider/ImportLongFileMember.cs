using LibProvider.COFF;
using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;

namespace LibProvider
{
    internal class ImportLongFileMember : ImportFileMember
    {
        private readonly IMAGE_LONG_IMPORT_HEADER _header;
        private readonly uint _fileContentStartPosition;
        private readonly IList<Section> _sections;
        private readonly IList<string> _strings;
        private IList<IMAGE_SYMBOL_ENTRY>? _symbols;

        internal ImportLongFileMember(MemoryMappedViewStream from, LongNameMember? nameCatalog,
            ReaderProvider.DebugFlags debugFlags)
            : base(from, nameCatalog, debugFlags)
        {
            // This offset is to be used for adjustment of various offsets in header.
            _fileContentStartPosition = base._startOffset + ArchivedFile.Header.InFileHeaderSize;
            _header = new IMAGE_LONG_IMPORT_HEADER(from);
            if (0 != _header.SizeOfOptionalHeader) {
                throw new ParsingException("Optional header not supported.");
            }
            if (0 == _header.NumberOfSections) {
                throw new ParsingException("Unexpected 0 sections count.");
            }
            Section[] sections = new Section[Utils.SafeCastToInt32(_header.NumberOfSections)];
            for(uint sectionIndex = 0; sectionIndex < _header.NumberOfSections; sectionIndex++) {
                sections[sectionIndex] = new Section(from, this, debugFlags);
            }
            _sections = sections.ToImmutableArray();

            // Symbol table. Adjust file offset first.
            from.Position = _fileContentStartPosition + _header.PointerToSymbolTable;
            List<IMAGE_SYMBOL_ENTRY> symbols = new List<IMAGE_SYMBOL_ENTRY>();
            // Must be 0x0x235BC
            uint symbolTableStartOffset = _fileContentStartPosition + _header.PointerToSymbolTable;
            bool traceSymbols = (0 < _header.NumberOfSymbols)
                && Utils.IsDebugFlagEnabled(ReaderProvider.DebugFlags.TraceSymbols, debugFlags);
            if (traceSymbols) {
                Utils.DebugTrace("SYMBOLS");
            }
            for (int index = 0; index < _header.NumberOfSymbols; index++) {
                IMAGE_SYMBOL_ENTRY scannedSymbol = new IMAGE_SYMBOL_ENTRY(from);
                if (traceSymbols) {
                    scannedSymbol.Dump("\t");
                }
                //if ((1 > scannedSymbol.SectionNumber) || (_header.NumberOfSections < scannedSymbol.SectionNumber)) {
                //    throw new ParsingException(
                //        $"Out-of-range symbol section index {scannedSymbol.SectionNumber}.");
                //}
                symbols.Add(scannedSymbol);
            }
            _symbols = symbols.ToImmutableArray();

            // String table
            // Must be 0x2364C
            from.Position = symbolTableStartOffset +
                (_header.NumberOfSymbols * IMAGE_SYMBOL_ENTRY.InFileEntrySize);
            // Remark : string table length includes the 4 bytes length value itself.
            long stringTableStartOffset = from.Position;
            uint stringTableLength = Utils.ReadLittleEndianUInt32(from);
            if (sizeof(uint) >= stringTableLength) {
                throw new ParsingException($"Invalid string table length value {stringTableLength}");
            }
            List<string> strings = new List<string>();
            while ((from.Position - stringTableStartOffset) < stringTableLength) {
                strings.Add(Utils.ReadNullTerminatedASCIIString(from));
            }
            _strings = strings.ToImmutableArray();
            return;
        }

        internal override string ArchivedFileTypeName => "Long import file";
    }
}