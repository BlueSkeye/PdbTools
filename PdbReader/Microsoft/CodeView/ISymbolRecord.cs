
namespace PdbReader.Microsoft.CodeView
{
    internal interface ISymbolRecord : ICodeviewRecord
    {

        new ICodeviewRecord.RecordType Type => RecordType.Symbol;
    }
}
