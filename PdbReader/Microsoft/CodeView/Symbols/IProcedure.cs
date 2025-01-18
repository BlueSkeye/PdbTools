
namespace PdbReader.Microsoft.CodeView.Symbols
{
    public interface IProcedure
    {
        string Name { get; }

        uint TypeOrID { get; }
    }
}
