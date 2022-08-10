using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace PdbReader
{
    public class Pdb
    {
        internal const string DebuggedPdbName = "vcruntime140d.amd64.pdb";
        private const string SymbolCacheRelativePath = @"AppData\Local\Temp\SymbolCache";
        private DebugInformationStream _debugInfoStream;
        /// <summary>Only valid for a short period of time during object initialization.
        /// Moreover this map is not loaded if strict checks are not enabled.</summary>
        private bool[]? _freeBlockMaps = null;
        private MemoryMappedFile _mappedPdb;
        private MemoryMappedViewAccessor _mappedPdbView;
        internal readonly FileInfo _pdbFile;
        private List<List<uint>> _streamDescriptors = new List<List<uint>>();
        private Dictionary<string, uint> _streamIndexByName;
        private uint[] _streamSizes;
        private bool _strictChecksEnabled;
        private readonly MSFSuperBlock _superBlock;
        private readonly TraceFlags _traceFlags;

        public Pdb(FileInfo target, TraceFlags traceFlags = 0, bool strictChecks = false)
        {
            _pdbFile = target ?? throw new ArgumentNullException(nameof(target));
            _traceFlags = traceFlags;
            if (!_pdbFile.Exists) {
                throw new ArgumentException($"Input file doesn't exist : '{_pdbFile.FullName}'");
            }
            _strictChecksEnabled = strictChecks;
            // Map the PDB file in memory.
            try {
                _mappedPdb = MemoryMappedFile.CreateFromFile(_pdbFile.FullName,
                    FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                _mappedPdbView = _mappedPdb.CreateViewAccessor(0, _pdbFile.Length,
                    MemoryMappedFileAccess.Read);
            }
            catch (Exception ex) {
                throw new PDBFormatException("Unable to map PDB file.", ex);
            }
            // Read super block and verify signature.
            try { _mappedPdbView.Read(0, out _superBlock); }
            catch (Exception ex){
                throw new PDBFormatException("Unable to read PDB superblock.", ex);
            }
            _superBlock.AssertSignature();
            if (_strictChecksEnabled) {
                LoadFreeBlocksMap();
            }
            uint[] streamDirectoryBlocks = LoadStreamDirectory();
            if (_strictChecksEnabled) {
                CheckBlocksMappingConsistency(streamDirectoryBlocks);
                _freeBlockMaps = null;
            }
            // TODO : Partially completed
            // LoadInfoStream();
            _debugInfoStream = new DebugInformationStream(this);
        }

        public DebugInformationStream DebugInfoStream
            => _debugInfoStream ?? throw new BugException();

        internal bool FullDecodingDebugEnabled
            => (0 != (_traceFlags & TraceFlags.FullDecodingDebug));

        internal bool ShouldTraceNamedStreamMap
            => (0 != (_traceFlags & TraceFlags.NamedStreamMap));

        internal bool ShouldTraceModules
            => (0 != (_traceFlags & TraceFlags.ModulesInformation));

        internal bool ShouldTraceStreamDirectory
            => (0 != (_traceFlags & TraceFlags.StreamDirectoryBlocks));

        public bool StrictChecksEnabled
        {
            get { return _strictChecksEnabled; }
            set { _strictChecksEnabled = value; }
        }

        internal MSFSuperBlock SuperBlock => _superBlock;

        internal void AssertValidStreamNumber(uint candidate)
        {
            if (!IsValidStreamNumber(candidate)) {
                throw new PDBFormatException($"Invalid stream number #{candidate} encountered.");
            }
        }

        internal static uint Ceil(uint value, uint dividedBy)
        {
            if (0 == dividedBy) {
                throw new ArgumentException(nameof(dividedBy));
            }
            if (0 == value) { return 0; }
            return 1 + ((value - 1) / dividedBy);
        }

        private void CheckBlocksMappingConsistency(uint[] streamDirectoryBlocks)
        {
            if (null == _freeBlockMaps) {
                throw new InvalidOperationException();
            }
            if (null == _streamSizes) {
                throw new InvalidOperationException();
            }
            if (null == _streamDescriptors) {
                throw new InvalidOperationException();
            }
            bool[] alreadySeenBlock = new bool[_freeBlockMaps.Length];
            // Superblock itself ...
            alreadySeenBlock[0] = true;
            // .. as well as those Free Block Maps blocks effectively used.
            uint totalBlocksCount = (uint)(1U + (uint)(((ulong)((ulong)_mappedPdbView.Capacity - 1UL)) / (ulong)_superBlock.BlockSize));
            uint perFreeBlockMapBlocksCount = 8 * _superBlock.BlockSize;
            uint freeBlockMapBlocks = 1 + ((totalBlocksCount - 1) / perFreeBlockMapBlocksCount);
            for(uint fbmbIndex = 0; fbmbIndex < freeBlockMapBlocks; fbmbIndex++) {
                alreadySeenBlock[1 + (fbmbIndex * _superBlock.BlockSize)] = true;
                alreadySeenBlock[2 + (fbmbIndex * _superBlock.BlockSize)] = true;
            }
            // Block map address from superblock.
            alreadySeenBlock[_superBlock.BlockMapAddr] = true;
            foreach(uint blockIndex in streamDirectoryBlocks) {
                alreadySeenBlock[blockIndex] = true;
            }
            // Scan stream descriptors.
            for (int descriptorIndex = 0; descriptorIndex < _streamDescriptors.Count; descriptorIndex++) {
                // Descriptor 0 is a special case. This was the old stream directory which blocks don't
                // look to be registered as used in free block map. Skip this stream.
                if (0 == descriptorIndex) {
                    continue;
                }
                List<uint> streamBlocks = _streamDescriptors[descriptorIndex];
                int streamBlocksCount = streamBlocks.Count;
                for(int streamBlocksIndex = 0; streamBlocksIndex < streamBlocksCount; streamBlocksIndex++) {
                    uint blockNumber = streamBlocks[streamBlocksIndex];
                    if (alreadySeenBlock[blockNumber]) {
                        throw new PDBFormatException(
                            $"Block index {blockNumber} already seen.");
                    }
                    alreadySeenBlock[blockNumber] = true;
                }
            }
            // None of the seen blocks should be marked as free in free block map.
            // Notice : the super block itself, as well as Free Block Map blocks are
            // not marked as in use in thhe _inUseBlockMaps.
            for (int index = 1; index < alreadySeenBlock.Length; index++) {
                if (IsFreeBlockMapBlock(index)) {
                    continue;
                }
                if (alreadySeenBlock[index] == _freeBlockMaps[index]) {
                    throw new PDBFormatException(
                        $"Seen block #{index} is marked as free in free block map.");
                }
            }
            // Every non free blocks in free block map should have been seen.
            for(int index = 0; index < _freeBlockMaps.Length; index++) {
                if (IsFreeBlockMapBlock(index)) {
                    continue;
                }
                if (_freeBlockMaps[index] == alreadySeenBlock[index]) {
                    throw new PDBFormatException(
                        $"Non free block #{index} in map is not bound to any stream.");
                }
            }
        }

        private int ComputeBlocksCount()
        {
            int result = (int)(_mappedPdbView.Capacity / _superBlock.BlockSize);
            if ((result * _superBlock.BlockSize) != _mappedPdbView.Capacity) {
                throw new PDBFormatException("Invalid file length.");
            }
            return result;
        }

        /// <summary>Ensure the symbol cache directory exists otherwise create
        /// it.</summary>
        /// <returns>A descriptor for the cache directory.</returns>
        /// <exception cref="BugException"></exception>
        private static DirectoryInfo EnsureSymbolCacheDirectory()
        {
            string? userProfileDirectory = Environment.GetEnvironmentVariable("USERPROFILE");
            if (null == userProfileDirectory) {
                throw new BugException();
            }
            DirectoryInfo result = new DirectoryInfo(
                Path.Combine(userProfileDirectory, SymbolCacheRelativePath));
            if (!result.Exists) {
                result.Create();
                result.Refresh();
            }
            return result;
        }

        /// <summary></summary>
        /// <param name="buffer"></param>
        /// <param name="bufferOffset"></param>
        /// <param name="position"></param>
        /// <param name="fillSize"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <remarks>TODO Seek for a more optimized solution.</remarks>
        internal void FillBuffer(IntPtr buffer, int bufferOffset, uint position,
            uint fillSize)
        {
            if (int.MaxValue < fillSize) {
                throw new ArgumentOutOfRangeException(nameof(fillSize));
            }
            byte[] localBuffer = new byte[fillSize];
            _mappedPdbView.ReadArray<byte>(position, localBuffer, 0, (int)fillSize);
            Marshal.Copy(localBuffer, 0, IntPtr.Add(buffer, bufferOffset), (int)fillSize);
        }

        internal uint GetBlockOffset(uint blockNumber)
        {
            if (blockNumber >= _superBlock.NumBlocks) {
                throw new ArgumentOutOfRangeException(nameof(blockNumber));
            }
            ulong result = blockNumber * _superBlock.BlockSize;
            if (result > uint.MaxValue) {
                throw new OverflowException();
            }
            return (uint)result;
        }

        /// <summary>Returns an array of block indexes for the stream having the given
        /// index.</summary>
        /// <param name="streamIndex"></param>
        /// <param name="streamSize">On return this parameter is updated with the stream
        /// size in bytes.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal uint[] GetStreamMap(uint streamIndex, out uint streamSize)
        {
            if (_streamDescriptors.Count <= streamIndex) {
                throw new ArgumentOutOfRangeException(nameof(streamIndex));
            }
            streamSize = _streamSizes[streamIndex];
            return _streamDescriptors[(int)streamIndex].ToArray();
        }

        internal uint GetStreamSize(uint streamIndex)
        {
            if (_streamSizes.Length <= streamIndex) {
                throw new ArgumentOutOfRangeException(nameof(streamIndex));
            }
            return _streamSizes[streamIndex];
        }

        private string GetString(byte[] buffer, uint bufferOffset)
        {
            if (null == buffer) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (int.MaxValue < bufferOffset) {
                throw new ArgumentOutOfRangeException(nameof(bufferOffset));
            }
            uint bufferLength = (uint)buffer.Length;
            uint maxStringLength = bufferLength - bufferOffset;
            int stringLength = 0;
            while(0 != buffer[bufferOffset + stringLength]) {
                if (++stringLength > maxStringLength) {
                    throw new PDBFormatException(
                        $"Unterminated string found at offset {bufferOffset} in string buffer.");
                }
            }
            return Encoding.ASCII.GetString(buffer, (int)bufferOffset, stringLength);
        }

        internal bool IsFreeBlockMapBlock(int candidate)
        {
            switch(candidate % _superBlock.BlockSize) {
                case 1:
                case 2:
                    return true;
                default:
                    return false;
            }
        }

        internal bool IsValidStreamNumber(uint candidate)
        {
            return (candidate < _streamDescriptors.Count);
        }

        /// <summary>Load the free block maps.</summary>
        /// <exception cref="PDBFormatException"></exception>
        private void LoadFreeBlocksMap()
        {
            if (0 != ((ulong)_mappedPdbView.Capacity % _superBlock.BlockSize)) {
                throw new PDBFormatException("Invalid file size.");
            }
            uint totalBlocksCount = (uint)((ulong)_mappedPdbView.Capacity / _superBlock.BlockSize);
            if (2 > totalBlocksCount) {
                throw new PDBFormatException("File too small.");
            }
            uint realMapBlocksCount = 1 + ((totalBlocksCount - 2) / _superBlock.BlockSize);
            uint effectiveMapBlocksCount = 1 + (totalBlocksCount / (8 * _superBlock.BlockSize));
            // Initialization
            _freeBlockMaps = new bool[totalBlocksCount];
            byte[] currentMapBlock = new byte[_superBlock.BlockSize];
            int mapBlockOffset = 0;
            int currentMapBlockOffset =
                (int)(_superBlock.BlockSize * _superBlock.FreeBlockMapBlock);
            _mappedPdbView.ReadArray<byte>(currentMapBlockOffset, currentMapBlock, 0,
                (int)_superBlock.BlockSize);
            byte inputByte = currentMapBlock[mapBlockOffset++];
            int availableBits = 8;
            int blockMapIndex = 0;
            for (int blockIndex = 0; blockIndex < totalBlocksCount; blockIndex++) {
                if (0 >= availableBits) {
                    if (_superBlock.BlockSize <= mapBlockOffset) {
                        currentMapBlockOffset += (int)(_superBlock.BlockSize * _superBlock.BlockSize);
                        if (_mappedPdbView.Capacity <= (currentMapBlockOffset + _superBlock.BlockSize)) {
                            throw new PDBFormatException("Invalid map block offset.");
                        }
                        // Read new block and reset offset.
                        _mappedPdbView.ReadArray<byte>(currentMapBlockOffset, currentMapBlock, 0,
                            (int)_superBlock.BlockSize);
                        mapBlockOffset = 0;
                    }
                    inputByte = currentMapBlock[mapBlockOffset++];
                    availableBits = 8;
                }
                _freeBlockMaps[blockMapIndex++] = (0 != (inputByte & 0x01));
                inputByte >>= 1;
                availableBits--;
            }
        }

        /// <summary>Load the PDB info stream.</summary>
        private void LoadInfoStream()
        {
            // The PDB info stream is at fixed index 1.
            PdbStreamReader reader = new PdbStreamReader(this, 1);
            // Stream starts with an header ...
            PdbStreamHeader header = reader.Read<PdbStreamHeader>();
            if (StrictChecksEnabled) {
                if (!Enum.IsDefined(header.Version)) {
                    throw new PDBFormatException(
                        $"Invalid PDB stream header version {header.Version}");
                }
            }
            // ... followed by a length prefixed array of strings ...
            uint stringBufferLength = reader.ReadUInt32();
            byte[] stringBuffer = new byte[stringBufferLength];
            reader.Read(stringBuffer);
            // ... then by an <uint, uint> hash table where key is an index in
            // string buffer and value is a stream index.
            _streamIndexByName = new Dictionary<string, uint>();
            HashTableReader hashReader = new HashTableReader(this, 1, reader.Offset);
            Dictionary<uint, uint> hashValues = hashReader.ReadUInt32Table();

            // Build the dictionary.
            foreach (KeyValuePair<uint, uint> pair in hashValues) {
                // Extract name from stringBuffer
                string streamName = GetString(stringBuffer, pair.Key);
                _streamIndexByName.Add(streamName, pair.Value);
            }
        }

        /// <summary></summary>
        /// <returns>An array of block indexes used by the stream directory.</returns>
        /// <exception cref="BugException"></exception>
        private uint[] LoadStreamDirectory()
        {
            BlockMapReader mapReader = new BlockMapReader(this);
            uint totalReadBytes = 0;
            uint numStreams = mapReader.ReadUInt32();
            totalReadBytes += sizeof(uint);
            if (ShouldTraceStreamDirectory) {
                Console.WriteLine($"DBG : Expecting {numStreams} streams.");
            }
            _streamSizes = new uint[numStreams];
            for (int streamIndex = 0; streamIndex < numStreams; streamIndex++) {
                uint streamBlocksCount = mapReader.ReadUInt32();
                totalReadBytes += sizeof(uint);
                _streamSizes[streamIndex] = streamBlocksCount;
                // NOTE : Some streams such as stream #83 in vcruntime140d.amd64.pdb with hash value
                // 4636DD42F408275AEE31944E871539941
                // have been found to have uint.MaxValue for count. Assume this is an empty stream.
                if ((0 == streamBlocksCount) || (uint.MaxValue == streamBlocksCount)) {
                    Console.Write($"DBG : Stream #{streamIndex} is empty.");
                    // Make sure the count is registered as 0 even if uint.MaxValue was found.
                    _streamSizes[streamIndex] = 0;
                    continue;
                }
                if (ShouldTraceStreamDirectory) {
                    Console.WriteLine(
                        $"DBG : Stream #{streamIndex} is {_streamSizes[streamIndex]} bytes.");
                }
            }
            for (int streamIndex = 0; streamIndex < numStreams; streamIndex++) {
                List<uint> streamDescriptor = new List<uint>();
                _streamDescriptors.Add(streamDescriptor);
                uint streamSize = _streamSizes[streamIndex];
                uint streamBlocksCount = Ceil(streamSize, _superBlock.BlockSize);
                if (ShouldTraceStreamDirectory) {
                    Console.Write($"DBG : Stream #{streamIndex} ({streamBlocksCount} blocks) : ");
                }
                for (int blockIndex = 0; blockIndex < streamBlocksCount; blockIndex++) {
                    uint blockNumber = mapReader.ReadUInt32();
                    totalReadBytes += sizeof(uint);
                    streamDescriptor.Add(blockNumber);
                    if (ShouldTraceStreamDirectory) {
                        if (0 != blockIndex) { Console.Write(", "); }
                        Console.Write(blockNumber);
                    }
                }
                if (ShouldTraceStreamDirectory) { Console.WriteLine(); }
            }
            if (totalReadBytes != _superBlock.NumDirectoryBytes) {
                throw new BugException(
                    $"Stream directory should be {_superBlock.NumDirectoryBytes}. {totalReadBytes} were read.");
            }
            return mapReader.BlocksList;
        }

        internal void Read(uint position, byte[] into, uint offset, uint length)
        {
            if (int.MaxValue < offset) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (int.MaxValue < length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            _mappedPdbView.ReadArray(position, into, (int)offset, (int)length);
        }

        internal byte ReadByte(ref uint offset)
        {
            try { return _mappedPdbView.ReadByte(offset); }
            finally { offset += sizeof(byte); }
        }

        internal ushort ReadUInt16(ref uint offset)
        {
            try { return _mappedPdbView.ReadUInt16(offset); }
            finally { offset += sizeof(ushort); }
        }

        internal uint ReadUInt32(ref uint offset)
        {
            try { return _mappedPdbView.ReadUInt32(offset); }
            finally { offset += sizeof(uint); }
        }

        internal T Read<T>(long position)
            where T : struct
        {
            T result;
            _mappedPdbView.Read<T>(position, out result);
            return result;
        }

        internal static uint SafeCastToUint32(int value)
        {
            if (0 > value) { throw new BugException(); }
            return (uint)value;
        }

        [Flags()]
        public enum TraceFlags
        {
            None = 0,
            StreamDirectoryBlocks = 0x00000001,
            NamedStreamMap = 0x00000002,
            ModulesInformation = 0x00000004,
            FullDecodingDebug = 0x00000008
        }
    }
}