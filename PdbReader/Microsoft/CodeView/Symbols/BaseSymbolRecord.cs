
namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal abstract class BaseSymbolRecord : ISymbolRecord
    {
        /// <summary>Record length is the total number of effective bytes of this record, NOT including the
        /// record length field itself. However the record itself MUST use an exact multiple of 4 bytes.
        /// Additional padding MUST thus be applied at end of symbol decoding to support this.</summary>
        private readonly ushort _recordLength;

        protected BaseSymbolRecord(ushort recordLength, SymbolStream.SymbolKind symbolKind)
        {
            _recordLength = recordLength;
            Kind = symbolKind;
        }

        public SymbolStream.SymbolKind Kind { get; private set; }
    }
}
