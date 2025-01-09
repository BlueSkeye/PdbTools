
namespace PdbReader
{
    public abstract class BaseStream
    {
        protected readonly Pdb _owner;
        internal readonly PdbStreamReader _reader;
        private readonly ushort _streamIndex;

        protected BaseStream(Pdb owner, ushort streamIndex)
        {
            _streamIndex = streamIndex;
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _reader = new PdbStreamReader(owner, streamIndex);
            return;
        }

        internal PdbStreamReader Reader => _reader;

        internal ushort StreamIndex => _streamIndex;

        internal uint StreamSize => _owner.GetStreamSize(_streamIndex);

        internal abstract string StreamName { get; }
    }
}
