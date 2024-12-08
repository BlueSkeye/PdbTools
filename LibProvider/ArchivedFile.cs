
using System.IO.MemoryMappedFiles;
using System.Text;

namespace LibProvider
{
    internal class ArchivedFile
    {
        private readonly MemoryMappedViewStream _from;
        private readonly int _startOffset;

        internal ArchivedFile(MemoryMappedViewStream from, int offset)
        {
            _from = from;
            _startOffset = offset;
        }

        private struct Header
        {
            internal Header(MemoryMappedViewStream from)
            {
                const int FileSizeLength = 10;
                const int IdentifierLength = 16;
                const int ModificationTimestampStringLength = 12;
                const int OwnerAndGroupIdsStringLength = 6;

                Identifier = ASCIIEncoding.ASCII.GetString(
                    Utils.AllocateBufferAndAssertRead(from, IdentifierLength));
                ModificationTimestamp = int.Parse(ASCIIEncoding.ASCII.GetString(
                    Utils.AllocateBufferAndAssertRead(from, ModificationTimestampStringLength)).Trim());
                OwnerId = int.Parse(ASCIIEncoding.ASCII.GetString(
                    Utils.AllocateBufferAndAssertRead(from, OwnerAndGroupIdsStringLength)).Trim());
                FileMode = Utils.ParseOctalNumber(ASCIIEncoding.ASCII.GetString(
                    Utils.AllocateBufferAndAssertRead(from, OwnerAndGroupIdsStringLength)).Trim());
                FileSize = uint.Parse(ASCIIEncoding.ASCII.GetString(
                    Utils.AllocateBufferAndAssertRead(from, FileSizeLength)).Trim());
                if ((0x60 != from.ReadByte()) || (0x0A != from.ReadByte())) {
                    throw new ParsingException("Invalid header ending characters.");
                }
            }

            internal uint FileMode { get; private set; }

            /// <summary>Each archive file member begins on an even byte boundary; a newline is inserted
            /// between files if necessary. Nevertheless, the size given reflects the actual size of the
            /// file exclusive of padding.</summary>
            internal uint FileSize { get; private set; }

            internal int GroupId { get; private set; }

            internal string Identifier { get; private set; }

            internal int ModificationTimestamp { get; private set; }

            internal int OwnerId { get; private set; }
        }
    }
}
