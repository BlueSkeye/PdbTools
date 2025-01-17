
namespace PdbReader.Microsoft.CodeView
{
    internal interface ISymbolRecord : ICodeviewRecord
    {
        BaseSymbolStream.SymbolKind Kind { get; }

        // Default implementation.
        ICodeviewRecord.RecordType ICodeviewRecord.Type => ICodeviewRecord.RecordType.Symbol;
    }
}
