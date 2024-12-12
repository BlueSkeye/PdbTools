using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace LibProvider
{
    /// <summary>See https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#second-linker-member</summary>
    internal class SecondLinkerMember : ArchivedFile
    {
        private long _dataStartOffset;

        internal SecondLinkerMember(MemoryMappedViewStream from, ReaderProvider.DebugFlags debugFlags)
            : base(from, null, debugFlags)
        {
            _dataStartOffset = from.Position;
            uint membersCount = Utils.ReadLittleEndianUInt32(from);
            uint[] memberOffsets = new uint[membersCount];
            for(int index = 0; membersCount > index; index++) {
                memberOffsets[index] = Utils.ReadLittleEndianUInt32(from);
            }
            MemberOffsets = memberOffsets.ToImmutableArray();
            uint symbolsCount = Utils.ReadLittleEndianUInt32(from);
            ushort[] symbolIndices = new ushort[symbolsCount];
            for(int index = 0; symbolsCount > index; index++) {
                symbolIndices[index] = Utils.ReadLittleEndianUShort(from);
            }
            SymbolIndices = symbolIndices.ToImmutableArray();
            StringBuilder builder = new StringBuilder();
            string[] symbolNames = new string[symbolsCount];
            for (int index = 0; symbolsCount > index; index++) {
                builder.Clear();
                while (true) {
                    int scannedByte = from.ReadByte();
                    if (0 > scannedByte) {
                        throw new ParsingException("EOF reached while reading a string.");
                    }
                    if (0 == scannedByte) {
                        break;
                    }
                    builder.Append((char)scannedByte);
                }
                symbolNames[index] = builder.ToString();
            }
            SymbolNames = symbolNames.ToImmutableArray();
            if (0 != (from.Position % 2)) {
                if (-1 == from.ReadByte()) {
                    throw new ParsingException("Missing padding byte.");
                }
            }
            if (base.ExpectedNextFileOffset != from.Position) {
                throw new ParsingException("Offset mismatch.");
            }
            return;
        }

        internal override string ArchivedFileTypeName => "second linker member";

        internal ImmutableArray<uint> MemberOffsets { get; private set; }

        internal ImmutableArray<ushort> SymbolIndices { get; private set; }

        internal ImmutableArray<string> SymbolNames { get; private set; }
    }
}
