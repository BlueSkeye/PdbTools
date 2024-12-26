
namespace PdbReader.Microsoft.CodeView
{
    internal interface ILeafRecord
    {
        /// <summary>Get the leaf record kind.</summary>
        LeafIndices LeafKind { get; }
    }
}
