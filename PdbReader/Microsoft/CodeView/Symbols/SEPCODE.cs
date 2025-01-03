
namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal class SEPCODE : BaseSymbolRecord
    {
        private readonly uint _parent; // pointer to the parent
        private readonly uint _end; // pointer to this block's end
        private readonly uint _length; // count of bytes of this block
        private readonly Flags _flags; // flags
        private readonly uint _offset; // sect:off of the separated code
        private readonly uint _parentOffset; // sectParent:offParent of the enclosing scope
        private readonly ushort _sect;       //  (proc, block, or sepcode)
        private readonly ushort _sectParent;

        internal SEPCODE(PdbStreamReader reader, ushort recordLength, SymbolStream.SymbolKind symbolKind)
            : base(recordLength, symbolKind)
        {
            _parent = reader.ReadUInt32();
            _end = reader.ReadUInt32();
            _length = reader.ReadUInt32();
            _flags = (Flags)reader.ReadUInt32();
            _offset = reader.ReadUInt32();
            _parentOffset = reader.ReadUInt32();
            _sect = reader.ReadUInt16();
            _sectParent = reader.ReadUInt16();
            return;
        }

        [Flags()]
        internal enum Flags : uint
        {
            IsLexicalScope = 0x00000001, // S_SEPCODE doubles as lexical scope
            ReturnsToParent = 0x00000002, // code frag returns to parent
        }
    }
}
