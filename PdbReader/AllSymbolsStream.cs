
using PdbReader.Microsoft.CodeView;

namespace PdbReader
{
    internal class AllSymbolsStream : BaseSymbolStream, IAllSymbolStream
    {
        internal AllSymbolsStream(Pdb owner, ushort index)
            : base(owner, index)
        {
            base.LoadAllRecords();
        }

        internal override string StreamName => "All symbols";

        public IEnumerable<ISymbolRecord> EnumerateSymbols()
        {
            foreach(ISymbolRecord record in base._symbols) {
                yield return record;
            }
            yield break;
        }
    }
}
