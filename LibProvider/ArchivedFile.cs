using System.IO.MemoryMappedFiles;
using System.Text;

namespace LibProvider
{
    internal class ArchivedFile
    {
        private readonly MemoryMappedViewStream _from;
        private Header _header; 
        private readonly uint _startOffset;

        internal ArchivedFile(MemoryMappedViewStream from, LongNameMember? nameCatalog)
        {
            _from = from;
            _startOffset = Utils.SafeCastToUInt32(from.Position);
            _header = new Header(from, nameCatalog);
        }

        internal long ExpectedNextFileOffset
        {
            get {
                return _startOffset + ArchivedFile.Header.InFileHeaderSize + _header.FileSize +
                    ((0 != (_header.FileSize % 2)) ? 1 : 0);
            }
        }

        internal Header FileHeader => _header;

        /// <summary>Set memory mapped file stream position just after the file.</summary>
        internal ArchivedFile SkipFile()
        {
            _from.Position = _startOffset + Header.InFileHeaderSize + _header.FileSize;
            if (0 != (_header.FileSize % 2)) {
                _from.Position += 1;
            }
            return this;
        }


        /// <summary></summary>
        /// <remarks>See https://learn.microsoft.com/en-us/windows/win32/debug/pe-format#archive-member-headers</remarks>
        internal struct Header
        {
            internal const int IdentifierLength = 16;
            private const int FileModeLength = 8;
            private const int FileSizeLength = 10;
            private const int ModificationTimestampStringLength = 12;
            private const int OwnerAndGroupIdsStringLength = 6;
            internal static readonly uint InFileHeaderSize = 60;

            internal Header(MemoryMappedViewStream from, LongNameMember? nameCatalog)
            {
                Identifier = ASCIIEncoding.ASCII.GetString(
                    Utils.AllocateBufferAndAssertRead(from, IdentifierLength)).Trim();
                if (Identifier.StartsWith("/")) {
                    switch (Identifier) {
                        case "/":
                            break;
                        case "//":
                            break;
                        default:
                            string idNumberString = Identifier.Substring(1);
                            if (null == nameCatalog) {
                                throw new BugException(
                                    "No name catalog provided while encountering a numbered id.");
                            }
                            uint idNumber = uint.Parse(idNumberString);
                            Identifier = nameCatalog.GetNameByOffset(idNumber);
                            break;
                    }
                }
                else {
                    if (!Identifier.EndsWith("/")) {
                        throw new ParsingException($"Trailing slash missing in archived file {Identifier}");
                    }
                    Identifier = Identifier.Substring(0, Identifier.Length - 1);
                }
                ModificationTimestamp = Utils.ReadAndParseInt32(from, ModificationTimestampStringLength);
                OwnerId = Utils.ReadAndParseInt32(from, OwnerAndGroupIdsStringLength);
                GroupId = Utils.ReadAndParseInt32(from, OwnerAndGroupIdsStringLength);
                FileMode = Utils.ReadAndParseOctalUInt32(from, FileModeLength);
                FileSize = Utils.ReadAndParseUInt32(from, FileSizeLength);
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
    }
}
