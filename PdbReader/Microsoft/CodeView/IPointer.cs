
namespace PdbReader.Microsoft.CodeView
{
    internal interface IPointer : ITypeRecord
    {
        internal PointerBody Body { get; }
    }
}
