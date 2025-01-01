
namespace PdbReader.Microsoft.CodeView
{
    internal interface ITypeRecord : ICodeviewRecord
    {
        /// <summary>Get the leaf record kind.</summary>
        LeafIndices LeafKind { get; }
    }
}
