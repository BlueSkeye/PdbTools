
namespace PdbReader
{
    /// <summary>Also known as the TPI stream.</summary>
    public class TypeIndexedStream : IndexedStream
    {
        private const ushort ThisStreamIndex = 2;

        public TypeIndexedStream(Pdb owner)
            : base(owner, ThisStreamIndex)
        {
        }

        internal override string StreamName => "TPI";
    }
}
