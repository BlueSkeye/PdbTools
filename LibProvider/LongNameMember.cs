using System.Collections.Immutable;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace LibProvider
{
    /// <summary>See https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#longnames-member</summary>
    internal class LongNameMember : ArchivedFile
    {
        internal LongNameMember(MemoryMappedViewStream from, ReaderProvider.DebugFlags debugFlags)
            : base(from, null, debugFlags)
        {
            long fileStartOffset = from.Position;
            StringBuilder builder = new StringBuilder();
            List<uint> nameOffsets = new List<uint>();
            List<string> symbolNames = new List<string>();
            while (base.ExpectedNextFileOffset > from.Position) {
                nameOffsets.Add((uint)(from.Position - fileStartOffset));
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
                string symbolName = builder.ToString();
                if (string.Empty != symbolName) {
                    symbolNames.Add(builder.ToString());
                }
            }
            NamesOffset = nameOffsets.ToImmutableArray();
            MemberNames = symbolNames.ToImmutableArray();
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

        internal string GetNameByOffset(uint offset)
        {
            int namesCount = MemberNames.Count;
            for(int nameIndex = 0; namesCount > nameIndex; nameIndex++) {
                if (offset == NamesOffset[nameIndex]) {
                    return MemberNames[nameIndex];
                }
            }
            throw new ParsingException($"Can't find name offset {offset} in long names catalog.");
        }

        internal override string ArchivedFileTypeName => "long name member";

        internal IList<uint> NamesOffset { get; private set; }

        internal IList<string> MemberNames { get; private set; }
    }
}
