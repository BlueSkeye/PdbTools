
namespace PdbReader.Microsoft.CodeView
{
    public interface ISymbolRecord : ICodeviewRecord
    {
        SymbolKind Kind { get; }

        // Default implementation.
        ICodeviewRecord.RecordType ICodeviewRecord.Type => ICodeviewRecord.RecordType.Symbol;
    }
}
