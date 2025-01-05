using System.Runtime.InteropServices;

namespace PdbReader
{
    internal class HashTable
    {
        private const int HashSize = 4096;
        private Header _header;
        private List<HashRecord> _recordHash;

        ///<summary></summary>
        /// <param name="value"></param>
        /// <param name="alignment"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static ulong ComputeBitmapBitsCount(ulong value, ulong alignment)
        {
            if (0 == alignment) {
                throw new ArgumentException();
            }
            return (value + alignment - 1) / (alignment * alignment);
        }

        internal static HashTable Create(PdbStreamReader reader)
        {
            HashTable result = new HashTable() {
                _header = reader.Read<Header>(),
                _recordHash = new List<HashRecord>()
            };
            // Read header.
            if (Header._Signature.SupportedVersion != result._header.Signature) {
                throw new BugException();
            }
            // Read bitmap
            ulong bitmapBitsCount = ComputeBitmapBitsCount(HashSize + 1, 32);
            uint bitmapEntriesCount = (uint)(bitmapBitsCount / 32);
            uint[] bitmap = new uint[bitmapEntriesCount];
            reader.ReadArray<uint>(bitmap, reader.ReadUInt32);
            uint compressedBucketIndex = 0;
            uint[] _map = new uint[HashSize];
            uint bucketsCount = 0;
            for (int hashIndex = 0; hashIndex < HashSize; hashIndex++) {
                byte wordIndex = (byte)(hashIndex / 32);
                byte bitIndex = (byte)(hashIndex % 32);
                bool bitIsSet = (0 != (bitmap[wordIndex] & (1 << bitIndex)));
                if (bitIsSet) {
                    _map[hashIndex] = compressedBucketIndex++;
                    bucketsCount++;
                }
                else { _map[hashIndex] = uint.MaxValue; }
            }
            // Read buckets
            uint[] _hashBuckets = new uint[bucketsCount];
            reader.ReadArray<uint>(_hashBuckets, reader.ReadUInt32);

            uint hashCount = result._header.RecordsBytesCount / HashRecord.Size;
            for(int index = 0; index < hashCount; index++) {
                result._recordHash.Add(reader.Read<HashRecord>());
            }
            return result;
        }

        private static uint GetEnabledBitsCount(uint value)
        {
            uint result = 0;
            for(int index = 0; index < 32; index++) {
                if (0 != (value & (1 << index))) {
                    result++;
                }
            }
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct HashRecord
        {
            internal static uint Size = (uint)Marshal.SizeOf<HashRecord>();

            // Offset in symbol record stream.
            internal uint Offset;
            internal uint CRef;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Header
        {
            internal _Signature Signature;
            internal uint VerHdr;
            /// <summary>Number of bytes for records storage.</summary>
            internal uint RecordsBytesCount;
            /// <summary>Number of buckets.</summary>
            internal uint BucketsCount;

            internal enum _Signature : uint
            {
                HdrSignature = uint.MaxValue,
                SupportedVersion = 0xEFFE0000 + 19990810
            }
        }
    }
}
