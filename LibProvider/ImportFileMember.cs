using System.IO.MemoryMappedFiles;

namespace LibProvider
{
    internal abstract class ImportFileMember : ArchivedFile
    {
        internal ImportFileMember(MemoryMappedViewStream from, LongNameMember? nameCatalog,
            ReaderProvider.DebugFlags debugFlags)
            : base(from, nameCatalog, debugFlags)
        {
            StartPosition = Utils.SafeCastToUInt32(from.Position);
            return;
        }
        
        internal static ImportFileMember Create(MemoryMappedViewStream from, LongNameMember? nameCatalog,
            ReaderProvider.DebugFlags debugFlags)
        {
            // Enforce alignment rule.
            if (0 != (from.Position % 2)) {
                from.ReadByte();
            }
            uint startPosition = Utils.SafeCastToUInt32(from.Position);
            // Little trick will let us discover wether this is an EmbeddedFileMember or an ImportFileMember
            ushort machine;
            try {
                from.Position += ArchivedFile.Header.InFileHeaderSize;
                machine = Utils.ReadLittleEndianUShort(from);
            }
            finally { from.Position = startPosition; }
            bool isShortMember = (0 == machine);
            if (0 == machine) {
                ImportShortFileMember result = new ImportShortFileMember(from, nameCatalog, debugFlags);
                if (Utils.IsDebugFlagEnabled(ReaderProvider.DebugFlags.DumpShortFiles, debugFlags)) {
                    result.Dump("\t");
                }
                return result;
            }
            else {
                return new ImportLongFileMember(from, nameCatalog, debugFlags);
            }
        }

        internal uint StartPosition { get; private set; }
    }
}
