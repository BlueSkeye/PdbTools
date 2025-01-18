
namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal class END : BaseSymbolRecord
    {
        private static readonly Dictionary<Pdb, END> _endSymbolByPdb = new Dictionary<Pdb, END>();

        private END(Pdb owner)
            : base(owner, 2, SymbolKind.S_END)
        {
        }

        internal static void Release(Pdb owner)
        {
            if (_endSymbolByPdb.ContainsKey(owner)) {
                _endSymbolByPdb.Remove(owner);
            }
        }

        internal static END GetENDSymbolFor(Pdb owner)
        {
            END result;
            if (!_endSymbolByPdb.TryGetValue(owner, out result)) {
                result = new END(owner);
            }
            return result;
        }
    }
}
