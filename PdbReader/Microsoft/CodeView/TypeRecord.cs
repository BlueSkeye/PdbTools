
namespace PdbReader.Microsoft.CodeView
{
    internal abstract class TypeRecord : ITypeRecord
    {
        public abstract LeafIndices LeafKind { get; }

        public ICodeviewRecord.RecordType Type => ICodeviewRecord.RecordType.Type;
    }
}
