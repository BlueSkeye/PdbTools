
namespace PdbReader
{
    internal class AllSymbolsStream : BaseSymbolStream
    {
        internal AllSymbolsStream(Pdb owner, ushort index)
            : base(owner, index)
        {
            base.LoadAllRecords();
        }

        internal override string StreamName => "All symbols";
    }
}
