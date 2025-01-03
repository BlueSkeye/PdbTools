
namespace PdbReader.Microsoft.CodeView
{
    internal interface ISymbolRecord : ICodeviewRecord
    {
        SymbolStream.SymbolKind Kind { get; }

        // Default implementation.
        ICodeviewRecord.RecordType ICodeviewRecord.Type => ICodeviewRecord.RecordType.Symbol;
    }
}
