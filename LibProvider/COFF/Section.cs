using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;

namespace LibProvider.COFF
{
    internal class Section
    {
        private static readonly ImmutableArray<IMAGE_RELOCATION_ENTRY> NoRelocation =
            new IMAGE_RELOCATION_ENTRY[0].ToImmutableArray();
        private static readonly ImmutableArray<byte> NoRowData = new byte[0].ToImmutableArray();
        internal readonly IMAGE_SECTION_HEADER Header;
        internal readonly ImportLongFileMember Owner;
        private ImmutableArray<byte> _rawData;
        private ImmutableArray<IMAGE_RELOCATION_ENTRY> _relocations;

        /// <summary>On return, stream position is just after the section header.</summary>
        /// <param name="from"></param>
        /// <param name="owner"></param>
        /// <param name="traceFlags"></param>
        internal Section(MemoryMappedViewStream from, ImportLongFileMember owner,
            ReaderProvider.DebugFlags traceFlags)
        {
            Owner = owner;
            if (Utils.IsDebugFlagEnabled(ReaderProvider.DebugFlags.TraceArchiveFileMemberSectionsOffset,
                traceFlags))
            {
                Utils.DebugTrace($"New section found @0x{from.Position:X8}.");
            }
            Header = new IMAGE_SECTION_HEADER(from, traceFlags);
            if (Utils.IsDebugFlagEnabled(ReaderProvider.DebugFlags.TraceArchiveFileMemberSectionsData, traceFlags)) {
                Header.Dump("\t");
            }
            long savedPosition = from.Position;
            try {
                if (0 >= Header.sizeOfRawData) {
                    _rawData = NoRowData;
                }
                else {
                    byte[] rawData = new byte[Header.sizeOfRawData];
                    from.Position = owner.StartPosition + Header.pointerToRawData;
                    from.Read(rawData, 0, rawData.Length);
                    _rawData = rawData.ToImmutableArray();
                    if (Utils.IsDebugFlagEnabled(ReaderProvider.DebugFlags.DumpSectionRawData, traceFlags)) {
                        Utils.Dump("\t", "Raw data", _rawData);
                    }
                }
                bool dump = Utils.IsDebugFlagEnabled(ReaderProvider.DebugFlags.DumpRelocationData, traceFlags);
                if (0 >= Header.numberOfRelocations) {
                    _relocations = NoRelocation;
                    if (dump) {
                        Utils.DebugTrace("\tNo relocation.");
                    }
                }
                else {
                    List<IMAGE_RELOCATION_ENTRY> relocations = new List<IMAGE_RELOCATION_ENTRY>();
                    if (dump) {
                        Utils.DebugTrace("\tRelocations.");
                    }
                    for (int index = 0; index < Header.numberOfRelocations; index++) {
                        IMAGE_RELOCATION_ENTRY relocationEntry = new IMAGE_RELOCATION_ENTRY(from);
                        relocations.Add(relocationEntry);
                        if (dump) {
                            relocationEntry.Dump("\t");
                        }
                    }
                    _relocations = relocations.ToImmutableArray();
                }
                Relocations = _relocations;
                return;
            }
            finally { from.Position = savedPosition; }
        }

        internal ImmutableArray<IMAGE_RELOCATION_ENTRY> Relocations { get; private set; }
    }
}
