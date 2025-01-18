using PdbReader.Microsoft.CodeView;

namespace PdbReader
{
    internal interface IAllSymbolStream
    {
        IEnumerable<ISymbolRecord> EnumerateSymbols();
    }
}
