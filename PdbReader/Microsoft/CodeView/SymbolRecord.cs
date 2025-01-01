
namespace PdbReader.Microsoft.CodeView
{
    internal abstract class SymbolRecord : ICodeviewRecord
    {
        public ICodeviewRecord.RecordType Type => ICodeviewRecord.RecordType.Symbol;
    }
}
