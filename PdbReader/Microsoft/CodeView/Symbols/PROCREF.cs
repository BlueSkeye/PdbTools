
namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal class PROCREF : BaseSymbolRecord
    {
        internal uint _sumName;
        internal uint _symOffset;
        internal ushort _module;

        internal PROCREF(PdbStreamReader reader, ushort recordLength, BaseSymbolStream.SymbolKind kind)
            : base(recordLength, kind)
        {
            _sumName = reader.ReadUInt32();
            _symOffset = reader.ReadUInt32();
            _module = reader.ReadUInt16();
            Name = reader.ReadNTBString();
            reader.EnsureAlignment(sizeof(uint));
            return;
        }

        internal string Name { get; private set; }
    }
}
