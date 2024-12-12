using LibProvider.COFF;
using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;

namespace LibProvider
{
    internal class ImportLongFileMember : ImportFileMember
    {
        private readonly IMAGE_FILE_HEADER _header;
        private readonly ImmutableArray<Section> _sections;
        private readonly ImmutableArray<string> _strings;

        internal ImportLongFileMember(MemoryMappedViewStream from, LongNameMember? nameCatalog,
            ReaderProvider.DebugFlags debugFlags)
            : base(from, nameCatalog, debugFlags)
        {
            // This offset is to be used for adjustment of various offsets in header.
            uint fileContentStartPosition = base._startOffset + ArchivedFile.Header.InFileHeaderSize;
            _header = new IMAGE_FILE_HEADER(from);
            if (0 != _header.SizeOfOptionalHeader) {
                throw new ParsingException("Optional header not supported.");
            }
            if (0 == _header.NumberOfSections) {
                throw new ParsingException("Unexpected 0 sections count.");
            }
            Section[] sections = new Section[Utils.SafeCastToInt32(_header.NumberOfSections)];
            for(uint sectionIndex = 0; sectionIndex < _header.NumberOfSections; sectionIndex++) {
                sections[sectionIndex] = new Section(from, debugFlags);
            }
            // Read sections data and relocations (if any).
            throw new NotImplementedException("TODO");

            // Symbol table. Adjust file offset first.
            from.Position = fileContentStartPosition + _header.PointerToSymbolTable;
            List<IMAGE_SYMBOL_ENTRY>[] symbols = new List<IMAGE_SYMBOL_ENTRY>[sections.Length];
            for(int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++) {
                symbols[sectionIndex] = new List<IMAGE_SYMBOL_ENTRY>();
            }
            if (0x23472 == _startOffset) {
                int i = 1;
            }
            for(int index = 0; index < _header.NumberOfSymbols; index++) {
                IMAGE_SYMBOL_ENTRY scannedSymbol = new IMAGE_SYMBOL_ENTRY(from);
                if ((1 > scannedSymbol.SectionNumber) || (_header.NumberOfSections < scannedSymbol.SectionNumber)) {
                    throw new ParsingException(
                        $"Out-of-range symbol section index {scannedSymbol.SectionNumber}.");
                }
                symbols[scannedSymbol.SectionNumber - 1].Add(scannedSymbol);
            }
            for(int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++) {
                sections[sectionIndex].Symbols = symbols[sectionIndex].ToImmutableArray();
            }
            _sections = sections.ToImmutableArray();

            // String table
            uint stringTableLength = Utils.ReadLittleEndianUInt32(from);
            long stringTableStartOffset = from.Position;
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