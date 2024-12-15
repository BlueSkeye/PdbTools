using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace LibProvider
{
    /// <summary>See https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#first-linker-member</summary>
    internal class FirstLinkerMember : ArchivedFile
    {
        private long _dataStartOffset;

        internal FirstLinkerMember(MemoryMappedViewStream from, ReaderProvider.DebugFlags debugFlags)
            : base(from, null, debugFlags)
        {
            _dataStartOffset = from.Position;
            uint membersCount = Utils.ReadBigEndianUInt32(from);
            uint[] offsets = new uint[membersCount];
            for(int index = 0; membersCount > index; index++) {
                offsets[index] = Utils.ReadBigEndianUInt32(from);
            }
            Offsets = offsets.ToImmutableArray();
            StringBuilder builder = new StringBuilder();
            string[] strings = new string[membersCount];
            for (int index = 0; membersCount > index; index++) {
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
                strings[index] = builder.ToString();
            }
            Strings = strings.ToImmutableArray();
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

        internal override string ArchivedFileTypeName => "first linker member";

        internal IList<uint> Offsets { get; private set; }

        internal IList<string> Strings { get; private set; }
    }
}
