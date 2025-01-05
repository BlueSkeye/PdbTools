
namespace PdbReader
{
    /// <summary>Allows for reading a serialized hash table starting at current position of an existing
    /// <see cref="PdbStreamReader"/>.</summary>
    internal class HashTableContent<T>
    {
        internal delegate T ValueReaderDelegate();
        private readonly Dictionary<uint, T> _content;

        private HashTableContent()
        {
            _content = new Dictionary<uint, T>();
        }

        internal static HashTableContent<T> Create(PdbStreamReader reader, ValueReaderDelegate valueReader)
        {
            uint hashTableSize = reader.ReadUInt32();
            uint hashTableCapacity = reader.ReadUInt32();
            uint bitVectorWordCount = reader.ReadUInt32();
            uint[] presentBucketsBitVector = new uint[bitVectorWordCount];
            for(int index = 0; index < bitVectorWordCount; index++) {
                presentBucketsBitVector[index] = reader.ReadUInt32();
            }
            uint deletedVectorWordCount = reader.ReadUInt32();
            for (int index = 0; index < deletedVectorWordCount; index++) {
                // We are not interested in the deleted vector bits content.
                // We could have used reader offset repositioning instead.
                reader.ReadUInt32();
            }
            HashTableContent<T> result = new HashTableContent<T>();
            Dictionary<uint, T> content = result._content;
            // Only the in use key/value pairs are stored in the hashtable. We are not really interested in
            // the bucket number of each pair, so let's ignore the index computation part (commented out and
            // incomplete however keep itfor later use if needed.).
            for (int index = 0; index < hashTableSize; index++) {
                //int bucketVectorIndex = index / 32;
                //int bucketVectorOffset = index % 32;
                //uint bucketVectorMask = 1U << bucketVectorOffset;
                //uint bucketVectorItemValue = presentBucketsBitVector[bucketVectorIndex];
                //uint bucketVectorMaskedItemValue = bucketVectorMask & bucketVectorItemValue;
                uint itemKey = reader.ReadUInt32();
                T itemValue = valueReader();
                //if (0 != bucketVectorMaskedItemValue) {
                    content.Add(itemKey, itemValue);
                //}
            }
            return result;
        }

        internal IEnumerable<KeyValuePair<uint, T>> Enumerate()
        {
            foreach (KeyValuePair<uint, T> pair in _content) {
                yield return pair;
            }
        }

        internal IEnumerable<uint> EnumerateKeys()
        {
            foreach (uint key in _content.Keys) {
                yield return key;
            }
        }

        internal IEnumerable<T> EnumerateValues()
        {
            foreach (T value in _content.Values) {
                yield return value;
            }
        }
    }
}
