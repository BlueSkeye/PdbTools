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
            uint startPosition = Utils.SafeCastToUInt32(from.Position);
            // Little trick will let us discover wether this is an EmbeddedFileMember or an ImportFileMember
            ushort machine;
            try {
                from.Position += ArchivedFile.Header.InFileHeaderSize;
                machine = Utils.ReadLittleEndianUShort(from);
            }
            finally { from.Position = startPosition; }
            return (0 == machine)
                ? new ImportShortFileMember(from, nameCatalog, debugFlags)
                : new ImportLongFileMember(from, nameCatalog, debugFlags);
        }

        internal uint StartPosition { get; private set; }
    }
}
