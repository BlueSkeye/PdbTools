
namespace PdbReader
{
    internal class HashTableReader : PdbStreamReader
    {
        private delegate T ValueReaderDelegate<T>();

        private readonly Pdb _pdb;
        private readonly uint _streamIndex;
        private readonly uint _tableStartOffset;

        internal HashTableReader(Pdb owner, uint streamIndex, uint startOffset = 0)
            : base(owner, streamIndex)
        {
            _pdb = owner ?? throw new ArgumentNullException(nameof(owner));
            _streamIndex = streamIndex;
            base.Offset = startOffset;
        }

        internal Dictionary<uint, uint> ReadUInt32Table()
        {
            return ReadTable<uint>(base.ReadUInt32);
        }

        private Dictionary<uint, T> ReadTable<T>(ValueReaderDelegate<T> valueReader)
        {
            uint hashTableSize = base.ReadUInt32();
            uint hashTableCapacity = base.ReadUInt32();
            uint bitVectorWordCount = base.ReadUInt32();
            uint[] presentBucketsBitVector = new uint[bitVectorWordCount];
            for(int index = 0; index < bitVectorWordCount; index++) {
                presentBucketsBitVector[index] = base.ReadUInt32();
            }
            uint deletedVectorWordCount = base.ReadUInt32();
            for (int index = 0; index < deletedVectorWordCount; index++) {
                // We are not interested in the deleted vector bits content.
                // We could have used reader offset repositioning instead.
                base.ReadUInt32();
            }
            Dictionary<uint, T> result = new Dictionary<uint, T>();
            for (int index = 0; index < hashTableSize; index++) {
                // Remark : bucket set/deleted bit are of no use for reading.
                //// Is the bucket used ?
                //int bucketVectorIndex = index / 32;
                //int bucketVectorOffset = index % 32;
                //uint bucketVectorMask = 1U << bucketVectorOffset;
                //uint bucketVectorItemValue = presentBucketsBitVector[bucketVectorIndex];
                //uint bucketVectorMaskedItemValue = bucketVectorMask & bucketVectorItemValue;
                uint itemKey = base.ReadUInt32();
                T itemValue = valueReader();
                //if (0 != bucketVectorMaskedItemValue) {
                    result.Add(itemKey, itemValue);
                //}
            }
            return result;
        }
    }
}
