
namespace PdbReader
{
    internal class PublicSymbolStream : IndexedStream
    {
        private uint[] _addressMap;
        private HashTable _hashTable;
        private PublicSymbolsStreamHeader _pssHeader;
        private uint[] _sections;
        private uint[] _thunks;

        public PublicSymbolStream(Pdb owner, ushort index)
            : base(owner, index)
        {
            // For debugging purpose.
            this.Reader.Offset = 0;
            _pssHeader = this.Reader.Read<PublicSymbolsStreamHeader>();
            _hashTable = HashTable.Create(this.Reader);

            // Read other stuff
            _addressMap = new uint[_pssHeader.AddressMapBytesCount / sizeof(uint)];
            this.Reader.ReadArray<uint>(_addressMap, this.Reader.ReadUInt32);
            _thunks = new uint[_pssHeader.ThunksCount];
            this.Reader.ReadArray<uint>(_thunks, this.Reader.ReadUInt32);
            _sections = new uint[_pssHeader.SectionsCount];
            this.Reader.ReadArray<uint>(_sections, this.Reader.ReadUInt32);
            return;
        }

        internal override string StreamName => "Public";
    }
}
