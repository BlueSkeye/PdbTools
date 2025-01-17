
using System.Reflection;

namespace PdbReader.Microsoft.CodeView.Symbols
{
    // TODO : Structure is unsure.
    internal class ANNOTATIONREF : BaseSymbolRecord
    {
        internal uint _unknown;
        internal uint _offset;
        internal ushort _module;

        internal ANNOTATIONREF(PdbStreamReader reader, ushort recordLength)
            : base(recordLength, BaseSymbolStream.SymbolKind.S_ANNOTATIONREF)
        {
            _unknown = reader.ReadUInt32();
            _offset = reader.ReadUInt32();
            _module = reader.ReadUInt16();
            return;
        }
    }
}
