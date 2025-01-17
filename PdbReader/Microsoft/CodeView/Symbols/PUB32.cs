
using System.Drawing;
using System;

namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal class PUB32 : BaseSymbolRecord
    {
        private uint _flags;
        private uint _offset;
        private ushort _segment;

        internal PUB32(PdbStreamReader reader, ushort recordLength)
            : base(recordLength, BaseSymbolStream.SymbolKind.S_PUB32)
        {
            _flags = reader.ReadUInt32();
            _offset = reader.ReadUInt32();
            _segment = reader.ReadUInt16();
            Name = reader.ReadNTBString();
            return;
        }

        internal string Name { get; private set; }
    }
}
