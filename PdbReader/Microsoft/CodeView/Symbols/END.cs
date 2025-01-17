
namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal class END : BaseSymbolRecord
    {
        internal static readonly END Singleton = new END();

        private END()
            : base(2, BaseSymbolStream.SymbolKind.S_END)
        {
        }
    }
}
