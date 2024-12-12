using LibProvider.COFF;
using System.IO.MemoryMappedFiles;

namespace LibProvider
{
    internal class ImportShortFileMember : ImportFileMember
    {
        private readonly string _dllName;
        private readonly IMAGE_SHORT_IMPORT_HEADER _header;
        private readonly string _importedName;

        internal ImportShortFileMember(MemoryMappedViewStream from, LongNameMember? nameCatalog,
            ReaderProvider.DebugFlags debugFlags)
            : base(from, nameCatalog, debugFlags)
        {
            _header = new IMAGE_SHORT_IMPORT_HEADER(from);
            _importedName = Utils.ReadNullTerminatedASCIIString(from);
            _dllName = Utils.ReadNullTerminatedASCIIString(from);
        }

        internal override string ArchivedFileTypeName => "Short import file";
    }
}
