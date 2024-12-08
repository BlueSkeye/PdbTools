
namespace PdbReader
{
    public interface IStreamGlobalOffset : IComparable<IStreamGlobalOffset>
    {
        uint Value { get; }

        IStreamGlobalOffset Add(uint offset);

        IStreamGlobalOffset Subtract(uint offset);

        IStreamGlobalOffset Subtract(IStreamGlobalOffset offset) => Subtract(offset.Value);
    }
}
