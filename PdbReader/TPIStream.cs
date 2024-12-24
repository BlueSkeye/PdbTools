using PdbReader.TypeRecords;

namespace PdbReader
{
    /// <summary>Also known as the TPI stream.</summary>
    public class TPIStream : IndexedStream
    {
        private const ushort ThisStreamIndex = 2;

        public TPIStream(Pdb owner)
            : base(owner, ThisStreamIndex)
        {
            LoadContent();
        }

        internal override string StreamName => "TPI";

        private void LoadContent()
        {
            uint firstExcludedIndex = base._header.TypeIndexEndExcluded;
            uint contentLength = base._header.TypeRecordBytes;
            uint contentStartOffset = _reader.Offset;
            for(uint typeIndex = base._header.TypeIndexBegin; typeIndex < firstExcludedIndex; typeIndex++) {
                if (contentLength <= (_reader.Offset - contentStartOffset)) {
                    throw new PDBFormatException("TPI content length overflow.");
                }
                TypeRecordHeader recordHeader;
                // Pre read type header.
                uint typeStartOffset = _reader.Offset;
                try { recordHeader = _reader.Read<TypeRecordHeader>(); }
                finally { _reader.Offset = typeStartOffset; }
                switch (recordHeader.RecordKind) {
                    case TypeRecordHeader._Kind.Modifier:
                        new ModifierRecord(_reader);
                        break;
                    case TypeRecordHeader._Kind.Pointer:
                        new PointerRecord(_reader);
                        break;
                    default:
                        throw new PDBFormatException(
                            $"Unrecognized type kind {recordHeader.RecordKind}=0x{((ushort)recordHeader.RecordKind):X4}");
                }
                _reader.EnsureAlignment(sizeof(uint));
                uint expectedOffset = (typeStartOffset + recordHeader.RecordLength + sizeof(ushort));
                if (expectedOffset != _reader.Offset) {
                    throw new PDBFormatException(
                        $"Type record length mismatch on record type {recordHeader.RecordKind}.\n" +
                        $"Expected offset 0x{expectedOffset:X8}, real 0x{_reader.Offset:X8}");
                }
            }
            if (contentLength > (_reader.Offset - contentStartOffset)) {
                throw new PDBFormatException("TPI content length underflow.");
            }
        }
    }
}
