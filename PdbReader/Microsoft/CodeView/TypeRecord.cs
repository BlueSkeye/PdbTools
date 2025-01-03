using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal abstract class TypeRecord : ITypeRecord
    {
        public abstract TypeKind LeafKind { get; }

        public ICodeviewRecord.RecordType Type => ICodeviewRecord.RecordType.Type;
    }
}
