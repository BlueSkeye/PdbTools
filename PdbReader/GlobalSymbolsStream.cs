using PdbReader.Microsoft.CodeView;

namespace PdbReader
{
    internal class GlobalSymbolsStream : HashStream
    {
        public GlobalSymbolsStream(Pdb owner, ushort index)
            : base(owner, index)
        {
            // Because we will need it later.
            owner.EnsureSymbolStreamIsLoaded();
        }

        internal override string StreamName => "GSI";

        internal List<KeyValuePair<uint, ISymbolRecord>> FindSymbol(string symbolName)
        {
            List<KeyValuePair<uint, ISymbolRecord>> result = new List<KeyValuePair<uint, ISymbolRecord>>();

            // Hash the name to figure out which bucket this goes into.
            uint expandedBucketIndex = HashStringV1(symbolName) % HashStream.IPHR_HASH;
            int compressedBucketIndex = base._bucketMap[expandedBucketIndex];
            if (-1 == compressedBucketIndex) {
                return result;
            }
            uint lastBucketIndex = Utils.SafeCastToUint32(base._hashBuckets.Length - 1);
            uint startRecordIndex = base._hashBuckets[compressedBucketIndex] / 12;
            uint endRecordIndex = 0;
            if (compressedBucketIndex < lastBucketIndex) {
                endRecordIndex = base._hashBuckets[compressedBucketIndex + 1];
            }
            else {
                // If this is the last bucket, it consists of all hash records until the end
                // of the HashRecords array.
                endRecordIndex = Utils.SafeCastToUint32(base._hashRecords.Length * 12);
            }
            endRecordIndex /= 12;
            if (endRecordIndex > base._hashRecords.Length) {
                throw new BugException();
            }
            while (startRecordIndex < endRecordIndex) {
                HashRecord PSH = _hashRecords[startRecordIndex];
                uint offset = PSH.Offset - 1;
                throw new NotImplementedException();
                //ISymbolRecord record = Symbols.readRecord(offset);
                //if (codeview::getSymbolName(record) == Name)
                //    result.Add(new KeyValuePair<uint, ISymbolRecord>(offset, record));
                //startRecordIndex++;
            }
            return result;
        }
    }
}