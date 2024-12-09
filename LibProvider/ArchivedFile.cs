using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace LibProvider
{
    internal class ArchivedFile
    {
        private readonly MemoryMappedViewStream _from;
        private Header _header; 
        private readonly uint _startOffset;

        internal ArchivedFile(MemoryMappedViewStream from)
        {
            _from = from;
            _startOffset = Utils.SafeCastToUInt32(from.Position);
            _header = new Header(from);
        }

        internal long ExpectedNextFileOffset
        {
            get {
                return _startOffset + ArchivedFile.Header.InFileHeaderSize + _header.FileSize +
                    ((0 != (_header.FileSize % 2)) ? 1 : 0);
            }
        }

        internal Header FileHeader => _header;
        
        private static int ReadAndParseInt32(MemoryMappedViewStream from, int inputLength)
        {
            string parsedString = ASCIIEncoding.ASCII.GetString(
                Utils.AllocateBufferAndAssertRead(from, inputLength)).Trim();
            return (string.Empty == parsedString) ? 0 : int.Parse(parsedString);
        }

        private static uint ReadAndParseOctalUInt32(MemoryMappedViewStream from, int inputLength)
        {
            string parsedString = ASCIIEncoding.ASCII.GetString(
                Utils.AllocateBufferAndAssertRead(from, inputLength)).Trim();
            return (string.Empty == parsedString) ? 0 : Utils.ParseOctalNumber(parsedString);
        }

        private static uint ReadAndParseUInt32(MemoryMappedViewStream from, int inputLength)
        {
            string parsedString = ASCIIEncoding.ASCII.GetString(
                Utils.AllocateBufferAndAssertRead(from, inputLength)).Trim();
            return (string.Empty == parsedString) ? 0 : uint.Parse(parsedString);
        }

        private static uint ReadBigEndianUInt32(MemoryMappedViewStream from)
        {
            uint result = 0;
            for(int index = 0; sizeof(uint) > index; index++) {
                result <<= 8;
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a big endian uint.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (byte)inputByte;
            }
            return result;
        }

        private static ushort ReadBigEndianUShort(MemoryMappedViewStream from)
        {
            ushort result = 0;
            for(int index = 0; sizeof(ushort) > index; index++) {
                result <<= 8;
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a big endian uint.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (byte)inputByte;
            }
            return result;
        }

        private static uint ReadLittleEndianUInt32(MemoryMappedViewStream from)
        {
            uint result = 0;
            for(int index = 0; sizeof(uint) > index; index++) {
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a big endian uint.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (uint)((byte)inputByte << (8 * index));
            }
            return result;
        }

        private static ushort ReadLittleEndianUShort(MemoryMappedViewStream from)
        {
            ushort result = 0;
            for(int index = 0; sizeof(ushort) > index; index++) {
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a big endian uint.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (ushort)((byte)inputByte << (8 * index));
            }
            return result;
        }

        /// <summary>Set memory mapped file stream position just after the file.</summary>
        internal ArchivedFile SkipFile()
        {
            _from.Position = _startOffset + Header.InFileHeaderSize + _header.FileSize;
            if (0 != (_header.FileSize % 2)) {
                _from.Position += 1;
            }
            return this;
        }

        /// <summary>See https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#first-linker-member</summary>
        internal class FirstLinkerMember : ArchivedFile
        {
            private long _dataStartOffset;

            internal FirstLinkerMember(MemoryMappedViewStream from)
                : base(from)
            {
                _dataStartOffset = from.Position;
                uint membersCount = ReadBigEndianUInt32(from);
                uint[] offsets = new uint[membersCount];
                for(int index = 0; membersCount > index; index++) {
                    offsets[index] = ReadBigEndianUInt32(from);
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

            internal ImmutableArray<uint> Offsets { get; private set; }

            internal ImmutableArray<string> Strings { get; private set; }
        }

        /// <summary></summary>
        /// <remarks>See https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#archive-member-headers</remarks>
        internal struct Header
        {
            internal const int IdentifierLength = 16;
            internal static readonly uint InFileHeaderSize = 60;

            internal Header(MemoryMappedViewStream from)
            {
                const int FileModeLength = 8;
                const int FileSizeLength = 10;
                const int ModificationTimestampStringLength = 12;
                const int OwnerAndGroupIdsStringLength = 6;
                string parsedString;

                Identifier = ASCIIEncoding.ASCII.GetString(
                    Utils.AllocateBufferAndAssertRead(from, IdentifierLength)).Trim();
                ModificationTimestamp = ReadAndParseInt32(from, ModificationTimestampStringLength);
                OwnerId = ReadAndParseInt32(from, OwnerAndGroupIdsStringLength);
                GroupId = ReadAndParseInt32(from, OwnerAndGroupIdsStringLength);
                FileMode = ReadAndParseOctalUInt32(from, FileModeLength);
                FileSize = ReadAndParseUInt32(from, FileSizeLength);
                if ((0x60 != from.ReadByte()) || (0x0A != from.ReadByte())) {
                    throw new ParsingException("Invalid header ending characters.");
                }
            }

            /// <summary>ST_MODE value from the C run-time function _wstat.</summary>
            internal uint FileMode { get; private set; }

            /// <summary>Each archive file member begins on an even byte boundary; a newline is inserted
            /// between files if necessary. Nevertheless, the size given reflects the actual size of the
            /// file exclusive of header and padding.</summary>
            internal uint FileSize { get; private set; }

            internal int GroupId { get; private set; }

            internal string Identifier { get; private set; }

            /// <summary>Number of seconds sinc 01/01/1970 UTC</summary>
            internal int ModificationTimestamp { get; private set; }

            internal int OwnerId { get; private set; }

            /// <summary>If at least the size of an header remains available after current position of the
            /// input stream, read and return header name. On return, the input stream position remains
            /// unchanged.</summary>
            /// <param name="from"></param>
            /// <returns></returns>
            internal static string? TryGetHeaderName(MemoryMappedViewStream from)
            {
                if (from.Length <= from.Position + Header.InFileHeaderSize) {
                    return null;
                } 
                long startOffset = from.Position;
                try {
                    return ASCIIEncoding.ASCII.GetString(
                        Utils.AllocateBufferAndAssertRead(from, IdentifierLength)).Trim();
                }
                finally { from.Position = startOffset; }
            }
        }

        /// <summary>See https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#longnames-member</summary>
        internal class LongNameMember : ArchivedFile
        {
            private long _dataStartOffset;

            internal LongNameMember(MemoryMappedViewStream from)
                : base(from)
            {
                StringBuilder builder = new StringBuilder();
                List<string> symbolNames = new List<string>();
                while (base.ExpectedNextFileOffset > from.Position) {
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

            internal ImmutableArray<string> MemberNames { get; private set; }
        }

        /// <summary>See https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#second-linker-member</summary>
        internal class SecondLinkerMember : ArchivedFile
        {
            private long _dataStartOffset;

            internal SecondLinkerMember(MemoryMappedViewStream from)
                : base(from)
            {
                _dataStartOffset = from.Position;
                uint membersCount = ReadLittleEndianUInt32(from);
                uint[] memberOffsets = new uint[membersCount];
                for(int index = 0; membersCount > index; index++) {
                    memberOffsets[index] = ReadLittleEndianUInt32(from);
                }
                MemberOffsets = memberOffsets.ToImmutableArray();
                uint symbolsCount = ReadLittleEndianUInt32(from);
                ushort[] symbolIndices = new ushort[symbolsCount];
                for(int index = 0; symbolsCount > index; index++) {
                    symbolIndices[index] = ReadLittleEndianUShort(from);
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

            internal ImmutableArray<uint> MemberOffsets { get; private set; }

            internal ImmutableArray<ushort> SymbolIndices { get; private set; }

            internal ImmutableArray<string> SymbolNames { get; private set; }
        }
    }
}
