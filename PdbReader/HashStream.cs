using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace PdbReader
{
    /// <summary>The base class for both the GSI (global symbols) and PSS (public symbols stream)</summary>
    internal abstract class HashStream : BaseStream
    {
        private const int IPHR_HASH = 4096;
        private Header _header;
        private HashRecord[] _hashRecords;
        private uint[] _hashBitmap;
        private uint[] _hashBuckets;
        private int[] _bucketMap;

        public HashStream(Pdb owner, ushort streamIndex)
            : base(owner, streamIndex)
        {
            _header = _reader.Read<Header>();
            if (Header.SupportedVersion != _header.VerHdr) {
                throw new BugException(
                    $"Header version 0x{_header.VerHdr:X8} doesn't match expected version 0x{Header.SupportedVersion:X8}.");
            }
            if (0 != (_header.RecordsBytesCount % HashRecord.Size)) {
                throw new BugException(
                    $"Invalid record bytes count 0x{_header.RecordsBytesCount} in hashtable header..");
            }
            int hashRecordsCount = Utils.SafeCastToInt32(_header.RecordsBytesCount / HashRecord.Size);
            _hashRecords = new HashRecord[hashRecordsCount];
            for(int index = 0; index < hashRecordsCount; index++) {
                _hashRecords[index] = new HashRecord() {
                    Offset = _reader.ReadUInt32(),
                    CRef = _reader.ReadUInt32()
                };
            }
            // Before the actual hash buckets, there is a bitmap of length determined by IPHR_HASH.
            int bitmapSizeInBits = ComputeBitmapSize(IPHR_HASH + 1, 32);
            int bitmapEntryCount = bitmapSizeInBits / 32;
            _hashBitmap = new uint[bitmapEntryCount];
            for (int index = 0; index < bitmapEntryCount; index++) {
                _hashBitmap[index] = _reader.ReadUInt32();
            }
            _bucketMap = new int[IPHR_HASH + 1];
            int compressedBucketIdx = 0;
            for (int index = 0; index <= IPHR_HASH; index++) {
                byte WordIdx = Utils.SafeCastToByte(index / 32);
                byte BitIdx = Utils.SafeCastToByte(index % 32);
                bool IsSet = (0 != (_hashBitmap[WordIdx] & (1U << BitIdx)));
                _bucketMap[index] = (IsSet) ? compressedBucketIdx++ : -1;
            }
            uint bucketsCount = 0;
            foreach (uint B in _hashBitmap) {
                bucketsCount = Utils.SafeCastToUint32((ulong) bucketsCount + (ulong) popcount(B));
            }
            _hashBuckets = new uint[bucketsCount];
            for (int index = 0; index < bucketsCount; index++) {
                _hashBuckets[index] = _reader.ReadUInt32();
            }
            // We should have reached the end of the stream.
            if (StreamSize != _reader.Offset) {
                throw new PDBFormatException("End of stream offset mismatch.");
            }
            return;
        }

        // Returns a multiple of modulo needed to store size bytes.
        private int ComputeBitmapSize(int size, int modulo)
        {
            // The following line is equivalent to (size + modulo - 1) / modulo * modulo.

            // The division followed by a multiplication can be thought of as a right
            // shift followed by a left shift which zeros out the extra bits produced in
            // the bump; `~(modulo - 1)` is a mask where all those bits being zeroed out
            // are just zero.

            // Most compilers can generate this code but the pattern may be missed when
            // multiple functions gets inlined.
            return (size + modulo - 1) & ~(modulo - 1);
        }

        private uint popcount(uint value)
        {
            //; CHECK - LABEL: @popcount32(
            //; CHECK - NEXT:    [[TMP2:%.*]] = call i32 @llvm.ctpop.i32(i32[[TMP0:%.*]])
            //; CHECK - NEXT:    ret i32[[TMP2]]
            uint two = value >> 1;
            uint three = two & 0x55555555;
            uint four = value - three;
            uint five = four & 0x33333333;
            uint six = four >> 2;
            uint seven = six & 0x33333333;
            uint height = Utils.SafeCastToUint32((ulong)seven + (ulong)five);
            uint nine = height >> 4;
            uint ten = Utils.SafeCastToUint32((ulong)nine + (ulong)height);
            uint eleven = ten & 0x0F0F0F0F;
            uint twelve = eleven * 0x01010101;
            uint thirteen = twelve >> 24;
            return thirteen;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct HashRecord
        {
            internal const int Size = 8;

            internal uint Offset;
            internal uint CRef;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Header
        {
            internal const uint SupportedVersion = 0xEFFE0000 + 19990810;

            internal uint Signature;
            internal uint VerHdr;
            /// <summary>Number of bytes for records storage.</summary>
            internal uint RecordsBytesCount;
            /// <summary>Number of buckets.</summary>
            internal uint BucketsCount;
        }
    }
}
