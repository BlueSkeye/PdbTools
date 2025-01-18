
namespace PdbReader.Microsoft.CodeView.Symbols
{
    public interface IProcedureReference
    {
        ushort ModuleId { get; }

        string Name { get; }

        IProcedure GetProcedure();
    }
}
