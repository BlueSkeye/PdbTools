using LibProvider.COFF;
using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;

namespace LibProvider
{
    internal class ObjectFileMember : ArchivedFile
    {
        private readonly IMAGE_FILE_HEADER _header;
        private readonly ImmutableArray<Section> _sections;

        internal ObjectFileMember(MemoryMappedViewStream from, LongNameMember? nameCatalog)
            : base(from, nameCatalog)
        {
            _header = new IMAGE_FILE_HEADER(from);
            if (0 != _header.SizeOfOptionalHeader) {
                throw new ParsingException("Optional header not supported.");
            }
            if (0 == _header.NumberOfSections) {
                throw new ParsingException("Unexpected 0 sections count.");
            }
            Section[] sections = new Section[Utils.SafeCastToInt32(_header.NumberOfSections)];
            for(uint sectionIndex = 0; sectionIndex < _header.NumberOfSections; sectionIndex++) {
                sections[sectionIndex] = new Section(from);
            }
            _sections = sections.ToImmutableArray();
            long trash = from.Position;
            return;
        }

        /// <summary>Set memory mapped file stream position just after the file.</summary>
        internal ObjectFileMember SkipFile()
        {
            base._SkipFile();
            return this;
        }
    }
}