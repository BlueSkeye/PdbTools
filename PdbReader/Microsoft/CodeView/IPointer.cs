
namespace PdbReader.Microsoft.CodeView
{
    internal interface IPointer : ILeafRecord
    {
        internal PointerBody Body { get; }
    }
}
