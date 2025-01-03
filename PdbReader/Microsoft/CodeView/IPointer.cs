
using PdbReader.Microsoft.CodeView.Types;

namespace PdbReader.Microsoft.CodeView
{
    internal interface IPointer : ITypeRecord
    {
        internal PointerBody Body { get; }
    }
}
