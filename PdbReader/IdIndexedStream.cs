
namespace PdbReader
{
    /// <summary>Also known as the IPI stream.</summary>
    internal class IdIndexedStream : TypeIndexedStream
    {
        private const ushort ThisStreamIndex = 4;

        private IdIndexedStream(Pdb owner)
            : base(owner, ThisStreamIndex)
        {
        }

        internal static IdIndexedStream? Create(Pdb owner)
        {
            return owner.IsNonEmptyStream(ThisStreamIndex) ? new IdIndexedStream(owner) : null;
        }

        internal override string StreamName => "IPI";
    }
}
