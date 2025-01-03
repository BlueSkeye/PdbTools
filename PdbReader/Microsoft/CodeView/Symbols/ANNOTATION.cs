
namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal class ANNOTATION : BaseSymbolRecord
    {
        private readonly uint _offset;
        private readonly ushort _segment;
        private readonly ushort csz; // Count of zero terminated annotation strings
        private readonly string[] _annotations; // Sequence of zero terminated annotation strings

        internal ANNOTATION(PdbStreamReader reader, ushort recordLength, SymbolStream.SymbolKind symbolKind)
            : base(recordLength, symbolKind)
        {
            _offset = reader.ReadUInt32();
            _segment = reader.ReadUInt16();
            ushort annotationsCount = reader.ReadUInt16();
            _annotations = new string[annotationsCount];
            for(int index = 0; index < annotationsCount; index++) {
                _annotations[index] = reader.ReadNTBString();
            }
        }
    }
}
