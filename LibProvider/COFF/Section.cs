using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;

namespace LibProvider.COFF
{
    internal class Section
    {
        internal readonly IMAGE_SECTION_HEADER Header;
        private ImmutableArray<IMAGE_SYMBOL_ENTRY>? _symbols;

        internal Section(MemoryMappedViewStream from, ReaderProvider.DebugFlags traceFlags)
        {
            if (Utils.IsDebugFlagEnabled(ReaderProvider.DebugFlags.TraceObjectFileMemberSectionsOffset,
                traceFlags))
            {
                Utils.DebugTrace($"New section found @0x{from.Position:X8}.");
            }
            Header = new IMAGE_SECTION_HEADER(from);
            long savedPosition = from.Position;
            try {
                return;
            }
            finally { from.Position = savedPosition; }
        }

        internal ImmutableArray<IMAGE_SYMBOL_ENTRY> Symbols
        {
            get { return _symbols ?? throw new InvalidOperationException("Symbols not initialized."); }
            set
            {
                if (null != _symbols) {
                    throw new InvalidOperationException("Symbols already initialized.");
                }
                _symbols = value;
            }
        }
    }
}
