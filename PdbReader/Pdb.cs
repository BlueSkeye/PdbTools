using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Text;
using PdbReader.Microsoft.CodeView.Enumerations;

namespace PdbReader
{
    public class Pdb : IPdb
    {
        /// <summary>For debugging purpose. Allows for skipping files that don't match this name if not
        /// empty.</summary>
        internal const string DebuggedPdbName = "";
        private const string StringPoolStreamName = "/names";
        private AllSymbolsStream _allSymbolsStream;
        /// <summary>This member is only valid for a short period of time during object initialization.
        /// Moreover both the map and the count are not initialized unless strict checks areenabled.</summary>
        private uint _blockMapBlocksCount = 0;
        private DebugInformationStream _dbiStream;
        private PdbFeatureCodes[] _featureCodes;
        /// <summary>This member is only valid for a short period of time during object initialization.
        /// Moreover both the map and the count are not initialized unless strict checks areenabled.</summary>
        private bool[]? _freeBlockMaps = null;
        private GlobalSymbolsStream _globalStream;
        /// <summary>An array of flags describing blocks that are known to be in use.</summary>
        private bool[] _knownInUseBlocks;
        /// <summary>A memory mapping for the input PDB file.</summary>
        private MemoryMappedFile _mappedPdb;
        /// <summary>A view encompassing the full content of the <see cref="_mappedPdb"/> file.</summary>
        private MemoryMappedViewAccessor _mappedPdbView;
        internal readonly FileInfo _pdbFile;
        private Dictionary<uint, string> _pooledStringByOffset;
        /// <summary>The outermost list index is the stream index. The content is a list of block indexes
        /// that make the given stream.</summary>
        private List<List<uint>> _streamDescriptors = new List<List<uint>>();
        private Dictionary<string, uint> _streamIndexByName;
        private uint[] _streamBytesSizes;
        private bool _strictChecksEnabled;
        private readonly MSFSuperBlock _superBlock;
        private TPIStream _tpiStream;
        private readonly TraceFlags _traceFlags;

        /// <summary>Initialize a PDB reader by mapping the file and reading the superblock.</summary>
        /// <param name="target">Target PDB file to open.</param>
        /// <param name="traceFlags"></param>
        /// <param name="strictChecks">Whether to perform strict checks or not.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="PDBFormatException"></exception>
        private Pdb(FileInfo target, TraceFlags traceFlags = 0, bool strictChecks = false)
        {
            _pdbFile = target ?? throw new ArgumentNullException(nameof(target));
            _traceFlags = traceFlags;
            if (!_pdbFile.Exists) {
                throw new ArgumentException($"Input file doesn't exist : '{_pdbFile.FullName}'");
            }
            _strictChecksEnabled = strictChecks;
            // Map the PDB file in memory.
            try {
                _mappedPdb = MemoryMappedFile.CreateFromFile(_pdbFile.FullName, FileMode.Open, null, 0,
                    MemoryMappedFileAccess.Read);
                _mappedPdbView = _mappedPdb.CreateViewAccessor(0, _pdbFile.Length, MemoryMappedFileAccess.Read);
            }
            catch (Exception ex) {
                throw new PDBFormatException("Unable to map PDB file.", ex);
            }
            // Read super block.
            try { _mappedPdbView.Read(0, out _superBlock); }
            catch (Exception ex){
                throw new PDBFormatException("Unable to read PDB superblock.", ex);
            }
        }

        public DebugInformationStream DebugInfoStream => _dbiStream ?? throw new BugException();

        internal bool FullDecodingDebugEnabled => (0 != (_traceFlags & TraceFlags.FullDecodingDebug));

        internal bool FreeBlocksConsistencyDebugEnabled
            => (0 != (_traceFlags & TraceFlags.FreeBlocksConsistencyDebug));

        internal static bool IsDebuggingEnabled => !string.IsNullOrEmpty(DebuggedPdbName);

        internal bool MinimalDebugInfoEnabled { get; private set; }

        internal bool NoTypeMergeEnabled { get; private set; }

        internal bool ShouldTraceNamedStreamMap => (0 != (_traceFlags & TraceFlags.NamedStreamMap));

        internal bool ShouldTraceModules => (0 != (_traceFlags & TraceFlags.ModulesInformation));

        internal bool ShouldTraceStreamDirectory => (0 != (_traceFlags & TraceFlags.StreamDirectoryBlocks));

        public bool StrictChecksEnabled
        {
            get { return _strictChecksEnabled; }
            set { _strictChecksEnabled = value; }
        }

        /// <summary>Retrieve the index of the mandatory "/names" stream containing global strings.</summary>
        public uint StringPoolStreamIndex
        {
            get
            {
                uint result;
                if (null == _streamIndexByName) {
                    throw new BugException("Named stream map not yet loaded.");
                }
                if (!_streamIndexByName.TryGetValue(StringPoolStreamName, out result)) {
                    throw new PDBFormatException(
                        $"Mandatory stream '{StringPoolStreamName}' not found.");
                }
                return result;
            }
        }

        internal MSFSuperBlock SuperBlock => _superBlock;

        private void AssertDebugInformation()
        {
            if (null == _dbiStream) {
                throw new BugException("DBI stream should have been instantiated.");
            }
            return;
        }

        private void AssertStreamDescriptorsAreAvailable()
        {
            if ((null == _streamDescriptors) || (null == _streamBytesSizes)) {
                throw new BugException("Stream descriptors are not yet available.");
            }
        }

        internal void AssertValidStreamNumber(ushort? candidate, bool nonExistingIsValid = true)
        {
            if (null == candidate) {
                return;
            }
            if (!IsValidStreamNumber(candidate.Value)) {
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

        private void CheckBlocksMappingConsistency(uint blockMapBlocksCount)
        {
            if (null == _freeBlockMaps) {
                throw new InvalidOperationException();
            }
            AssertStreamDescriptorsAreAvailable();
            // .. as well as those Free Block Maps blocks effectively used.
            uint totalBlocksCount = (uint)(1U + (uint)(((ulong)((ulong)_mappedPdbView.Capacity - 1UL)) / (ulong)_superBlock.BlockSize));
            uint perFreeBlockMapBlocksCount = 8 * _superBlock.BlockSize;
            uint freeBlockMapBlocksCount = 1 + ((totalBlocksCount - 1) / perFreeBlockMapBlocksCount);
            // Block map address from superblock.
            if (FreeBlocksConsistencyDebugEnabled) {
                Console.WriteLine($"FBC : Block map address block #{_superBlock.BlockMapAddr} in use.");
            }
            _knownInUseBlocks[_superBlock.BlockMapAddr] = true;

            // None of the seen blocks should be marked as free in free block map.
            // Notice : the super block itself, as well as Free Block Map blocks are
            // not marked as in use in the _inUseBlockMaps.
            for (int index = 1; index < _knownInUseBlocks.Length; index++) {
                if (IsFreeBlockMapBlock(index)) {
                    continue;
                }
                if (_knownInUseBlocks[index] == _freeBlockMaps[index]) {
                    throw new PDBFormatException(
                        $"Seen block #{index} is marked as free in free block map.");
                }
            }
            // Every non free blocks in free block map should have been seen.
            for(int index = 0; index < _freeBlockMaps.Length; index++) {
                if (IsFreeBlockMapBlock(index)) {
                    continue;
                }
                if (_freeBlockMaps[index] == _knownInUseBlocks[index]) {
                    throw new PDBFormatException($"Non free block #{index} in map is not bound to any stream.");
                }
            }
        }

        /// <summary>Compute and return the total number of blocks within the <see cref="_mappedPdb"/> file,
        /// based on the block size found in PDB super block.</summary>
        /// <returns></returns>
        /// <exception cref="PDBFormatException"></exception>
        private int ComputeBlocksCount()
        {
            int result = (int)(_mappedPdbView.Capacity / _superBlock.BlockSize);
            if ((result * _superBlock.BlockSize) != _mappedPdbView.Capacity) {
                throw new PDBFormatException("Invalid file length.");
            }
            return result;
        }

        /// <summary>Create a <see cref="Pdb"/> object from the content of the given <paramref name="target"/>
        /// file. On return the PDB stream, the DBI stream, the TPI stream and the "/names" stream will have
        /// been loaded and their content cached.</summary>
        /// <param name="target">Input file.</param>
        /// <param name="traceFlags">A combination of trace flags to use for débugging/diagnostic purpose
        /// while parsing file content.</param>
        /// <param name="strictChecks">Wether or not to perform strict checks while parsing input file
        /// content.</param>
        /// <returns>A <see cref="Pdb"/> instance on successfull parsing or a null reference if any error
        /// is encountered or debugging mode is enabled and <paramref name="target"/> is not the file to
        /// be debugged.</returns>
        public static IPdb? Create(FileInfo target, TraceFlags traceFlags = 0, bool strictChecks = false)
        {
            if (IsDebuggedFile(target)) {
                Console.WriteLine($"Skipping {target.Name} for debugging purpose");
                return null;
            }
            Pdb result = new Pdb(target, traceFlags, strictChecks);
            // Verify signature.
            MSFSuperBlock.PdbKind pdbKind = result._superBlock.AssertSignatureType();
            if (MSFSuperBlock.PdbKind.DotNet == pdbKind) {
                Console.WriteLine($"Ignoring .Net PDB file.");
                return null;
            }
            if (result.FreeBlocksConsistencyDebugEnabled) {
                Console.WriteLine($"FBC : Super block #0 in use.");
            }
            result._knownInUseBlocks = new bool[result.ComputeBlocksCount()];
            result._knownInUseBlocks[0] = true;
            if (result._strictChecksEnabled) {
                result._blockMapBlocksCount = result.LoadFreeBlocksMap();
            }
            result.LoadStreamDirectory();
            if (result._strictChecksEnabled) {
                // TODO : Fix bug
                // result.CheckBlocksMappingConsistency(result._blockMapBlocksCount);
                result._freeBlockMaps = null;
            }
            result.LoadPdbStream();
            result._tpiStream = new TPIStream(result);
            result._dbiStream = new DebugInformationStream(result);
            result.EnsureNamesStreamIsLoaded();
            int noSymbolModuleCount = 0;
            foreach (ModuleInfoRecord moduleInfo in result._dbiStream.EnumerateModules()) {
                if (!moduleInfo.HasSymbolStream) {
                    noSymbolModuleCount++;
                    continue;
                }
                new ModuleInformationStream(result, moduleInfo.SymbolStreamIndex, moduleInfo.SymByteSize,
                    moduleInfo.C11ByteSize, moduleInfo.C13ByteSize);
            }
            Console.WriteLine($"INFO : {noSymbolModuleCount} symbol less modules found.");
            return result;
        }
        
        private void DBIHexaDump(StreamWriter writer, PdbStreamReader reader)
        {
            uint blockSize = _superBlock.BlockSize;
            uint chunksPerBlock = 16;
            uint chunkSize = chunksPerBlock * blockSize;
            byte[] buffer = new byte[chunkSize];
            uint remainingBytes = reader.StreamSize;
            StringBuilder builder = new StringBuilder();
            while (0 < remainingBytes) {
                uint readSize = Math.Min(remainingBytes, chunkSize);
                uint blockOffset = reader.GetGlobalOffset().Value;
                _mappedPdbView.ReadArray<byte>(blockOffset, buffer, 0, (int)readSize);
                reader.Offset += readSize;
                string dumpString = Utils.HexadecimalFormat(builder, blockOffset, buffer,
                    Utils.SafeCastToInt32(readSize)).ToString();
                writer.Write(dumpString);
                remainingBytes -= readSize;
                builder.Clear();
            }
            return;
        }
        
        public void DBIDump(StreamWriter writer, bool hexadump)
        {
            writer.WriteLine("DBI stream dump :");
            if (hexadump) {
                PdbStreamReader reader = new PdbStreamReader(this, 3);
                DBIHexaDump(writer, reader);
            }
            else {
                try { _dbiStream.Dump(writer); }
                finally { writer.Flush(); }
            }
            return;
        }

        public void DumpPublicSymbols(StreamWriter outputStream)
        {
            throw new NotImplementedException();
        }

        /// <summary>Make sure the clobal stream is loaded and its content cached.</summary>
        /// <remarks>See :
        /// C:\WORK\llvm-project\llvm\lib\DebugInfo\PDB\Native\GSIStreamBuilder.cpp and
        /// C:\WORK\llvm-project\llvm\lib\DebugInfo\PDB\Native\GlobalStream.cpp</remarks>
        public void EnsureGlobalStreamIsLoaded()
        {
            if (null != _globalStream) {
                return;
            }
            ushort globalSymbolsStreamIndex = Utils.SafeCastToUint16(_dbiStream.GlobalSymbolsStreamIndex);
            _globalStream = new GlobalSymbolsStream(this, globalSymbolsStreamIndex);
            return;
        }

        /// <summary>Make sure the mandatory "/names" stream is loaded and content is cached.</summary>
        /// <exception cref="PDBFormatException"></exception>
        private void EnsureNamesStreamIsLoaded()
        {
            if (null != _pooledStringByOffset) {
                return;
            }
            _pooledStringByOffset = new Dictionary<uint, string>();
            uint stringPoolStreamIndex = StringPoolStreamIndex;
            PdbStreamReader reader = new PdbStreamReader(this, stringPoolStreamIndex);
            uint streamSize = _streamBytesSizes[stringPoolStreamIndex];
#if DEBUG
            Console.WriteLine("POOLED STRINGS ==================");
#endif
            StringPoolHeader header = reader.Read<StringPoolHeader>();
            if (StringPoolHeader.StringPoolHeaderSignature != header.Signature) {
                throw new PDBFormatException($"Invalid string pool header signature 0x{header.Signature:X8}");
            }
            uint remainingBytes = header.ByteSize;
            while (0 < remainingBytes) {
                uint key = reader.Offset - StringPoolHeader.Size;
                string pooledString = reader.ReadNTBString(ref remainingBytes);
                _pooledStringByOffset.Add(key, pooledString);
#if DEBUG
                Console.WriteLine($"\t0x{key:X8} : '{pooledString}'");
#endif
            }
            return;
        }

        /// <summary>Make sure the symbol stream containing all symbols is loaded.</summary>
        /// <exception cref="NotImplementedException"></exception>
        public void EnsureSymbolStreamIsLoaded()
        {
            if (null != _allSymbolsStream) {
                return;
            }
            uint symbolStreamIndex = _dbiStream.SymbolRecordStreamIndex;
            uint symbolStreamSize = GetStreamSize(symbolStreamIndex);
            _allSymbolsStream = new AllSymbolsStream(this, Utils.SafeCastToUint16(symbolStreamIndex));
            //// WARNING : The second parameter is a delegate. We don't actually read an integer before invoking
            //// the method
            //HashTableContent<uint> hashTable = HashTableContent<uint>.Create(reader, reader.ReadUInt32);
            // We should have reached the end of the stream.
            return;
        }

        /// <summary>Enumerate all public symbols from the PDB file.</summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private IEnumerable<object> EnumeratePublicSymbols()
        {
            PublicSymbolStream publicSymbolStream = new PublicSymbolStream(this,
                Utils.SafeCastToUint16(_dbiStream.PublicSymbolsStreamIndex));
            throw new NotImplementedException();
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

        /// <remarks>See <see cref="IPdb.FindModuleById(uint)"/></remarks>
        public ModuleInfoRecord? FindModuleById(uint moduleId)
        {
            AssertDebugInformation();
            return _dbiStream.FindModuleById(moduleId);
        }

        /// <remarks>See <see cref="IPdb.FindModuleByRVA(uint)"/></remarks>
        public ModuleInfoRecord? FindModuleByRVA(uint relativeVirtualAddress)
        {
            AssertDebugInformation();
            return _dbiStream.FindModuleByRVA(relativeVirtualAddress);
        }

        /// <remarks>See <see cref="IPdb.FindSectionContribution(uint)"/></remarks>
        public SectionContributionEntry? FindSectionContribution(uint relativeVirtualAddress)
        {
            if (null == _dbiStream) {
                throw new BugException("DBI stream should have been instantiated.");
            }
            return _dbiStream.FindSectionContribution(relativeVirtualAddress);
        }
        
        /// <summary>Get the offset within the mapped PDB file of the block having the given number.
        /// Compuation is performed based on <see cref="BlockSize"/> value in the super block. First
        /// block has number 0.</summary>
        /// <param name="blockNumber"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="OverflowException"></exception>
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

        /// <remarks>See <see cref="IPdb.GetModuleFiles(uint)"/></remarks>
        public List<string> GetModuleFiles(uint moduleIndex)
        {
            List<string> result = new List<string>();
            ModuleInfoRecord? module = _dbiStream.FindModuleById(moduleIndex);

            if (null == module) {
                throw new ArgumentException($"Invalid module index #{moduleIndex}");
            }
            return result;
        }

        internal string GetPooledStringByOffset(uint offset)
        {
            EnsureNamesStreamIsLoaded();
            string? result;
            if (!_pooledStringByOffset.TryGetValue(offset, out result)) {
                throw new BugException($"Unable to retrieve pooled string @ 0x{offset:X8}");
            }
            return result;
        }

        /// <remarks>See <see cref="IPdb.GetSection(uint)"/></remarks>
        public SectionMapEntry GetSection(uint index) => _dbiStream.GetSection(index);

        /// <summary>Returns an array of block indexes for the stream having the given index.</summary>
        /// <param name="streamIndex"></param>
        /// <param name="streamSize">On return this parameter is updated with the stream size in bytes.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal uint[] GetStreamMap(uint streamIndex, out uint streamSize)
        {
            AssertStreamDescriptorsAreAvailable();
            if (_streamDescriptors.Count <= streamIndex) {
                throw new ArgumentOutOfRangeException(nameof(streamIndex));
            }
            streamSize = _streamBytesSizes[streamIndex];
            return _streamDescriptors[(int)streamIndex].ToArray();
        }

        /// <summary>Returns the size in bytes of the stream having the given index.</summary>
        /// <param name="streamIndex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal uint GetStreamSize(uint streamIndex)
        {
            AssertStreamDescriptorsAreAvailable();
            if (_streamBytesSizes.Length <= streamIndex) {
                throw new ArgumentOutOfRangeException(nameof(streamIndex));
            }
            return _streamBytesSizes[streamIndex];
        }

        //private string GetString(byte[] buffer, uint bufferOffset)
        //{
        //    if (null == buffer) {
        //        throw new ArgumentNullException(nameof(buffer));
        //    }
        //    if (int.MaxValue < bufferOffset) {
        //        throw new ArgumentOutOfRangeException(nameof(bufferOffset));
        //    }
        //    uint bufferLength = (uint)buffer.Length;
        //    uint maxStringLength = bufferLength - bufferOffset;
        //    int stringLength = 0;
        //    while(0 != buffer[bufferOffset + stringLength]) {
        //        if (++stringLength > maxStringLength) {
        //            throw new PDBFormatException(
        //                $"Unterminated string found at offset {bufferOffset} in string buffer.");
        //        }
        //    }
        //    return Encoding.ASCII.GetString(buffer, (int)bufferOffset, stringLength);
        //}

        /// <remarks>See <see cref="IPdb.InitializeSymbolsMap()"/></remarks>
        public void InitializeSymbolsMap()
        {
            foreach (object symbol in EnumeratePublicSymbols()) {
                bool doBreak = true;
            }
            return;
        }

        /// <summary>Does the <paramref name="candidate"/> file should be debugged.</summary>
        /// <param name="candidate">Candidate file.</param>
        /// <returns></returns>
        internal static bool IsDebuggedFile(FileInfo candidate)
        {
            return IsDebuggingEnabled && (0 == string.Compare(candidate.Name, DebuggedPdbName, true));
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

        internal bool IsNonEmptyStream(ushort streamNumber)
        {
            if (!IsValidStreamNumber(streamNumber)) {
                throw new BugException();
            }
            return (0 < _streamDescriptors[streamNumber].Count);
        }

        internal bool IsValidStreamNumber(ushort candidate)
        {
            return (candidate < _streamDescriptors.Count);
        }

        /// <summary>Load the free block maps.</summary>
        /// <returns>Total number of blocks used by the map. This is usefull for later blocks
        /// mapping conistency check (if enabled) because the map blocks other than the first
        /// one must be considered as in use.</returns>
        /// <exception cref="PDBFormatException"></exception>
        private uint LoadFreeBlocksMap()
        {
            if (0 != ((ulong)_mappedPdbView.Capacity % _superBlock.BlockSize)) {
                throw new PDBFormatException("Invalid file size.");
            }
            uint totalBlocksCount = (uint)ComputeBlocksCount();
            if (2 > totalBlocksCount) {
                throw new PDBFormatException("File too small.");
            }
            // Initialization
            _freeBlockMaps = new bool[totalBlocksCount];
            byte[] currentMapBlock = new byte[_superBlock.BlockSize];
            int mapBlockOffset = 0;
            uint currentMapBlockIndex = _superBlock.FreeBlockMapBlock;
            int currentMapBlockOffset =
                (int)(_superBlock.BlockSize * currentMapBlockIndex);
            if (FreeBlocksConsistencyDebugEnabled) {
                Console.WriteLine($"FCB : Current map block #{currentMapBlockIndex} is in use.");
            }
            _knownInUseBlocks[currentMapBlockIndex] = true;
            _mappedPdbView.ReadArray<byte>(currentMapBlockOffset, currentMapBlock, 0,
                (int)_superBlock.BlockSize);
            byte inputByte = currentMapBlock[mapBlockOffset++];
            int availableBits = 8;
            int blockMapIndex = 0;
            for (int blockIndex = 0; blockIndex < totalBlocksCount; blockIndex++) {
                if (0 >= availableBits) {
                    if (_superBlock.BlockSize <= mapBlockOffset) {
                        currentMapBlockIndex += _superBlock.BlockSize;
                        if (FreeBlocksConsistencyDebugEnabled) {
                            Console.WriteLine($"FBC : Free block map block #{currentMapBlockIndex} is in use.");
                        }
                        _knownInUseBlocks[currentMapBlockIndex] = true;
                        // Read a new free block map block.
                        currentMapBlockOffset = (int)(_superBlock.BlockSize * currentMapBlockIndex);
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
            uint effectiveMapBlocksCount = 1 + ((totalBlocksCount - 1) / (8 * _superBlock.BlockSize));
            return effectiveMapBlocksCount;
        }

        /// <summary>Load the PDB info stream.</summary>
        private void LoadPdbStream()
        {
            const uint PdbStreamIndex = 1;
            uint pdbStreamSize = GetStreamSize(PdbStreamIndex);
            // The PDB info stream is at fixed index 1.
            PdbStreamReader reader = new PdbStreamReader(this, PdbStreamIndex);
            // Stream starts with an header.
            PdbStreamHeader header = reader.Read<PdbStreamHeader>();
            if (StrictChecksEnabled) {
                if (!Enum.IsDefined(header.Version)) {
                    throw new PDBFormatException($"Invalid PDB stream header version {header.Version}");
                }
            }
            // Followed by a length prefixed array of NTB strings being the named streams list.
            uint stringBufferLength = reader.ReadUInt32();
            uint namedStreamsListStartOffset = reader.Offset;
            uint namedStreamsListEndOffsetExcluded = namedStreamsListStartOffset + stringBufferLength;

            // We capture string offsets because they are used as a key in the hashtable below.
            List<uint> namedStreamsBufferOffset = new List<uint>();
            List<string> namedStreams = new List<string>();
            while(namedStreamsListEndOffsetExcluded > reader.Offset) {
                namedStreamsBufferOffset.Add(reader.Offset - namedStreamsListStartOffset);
                string streamName = reader.ReadNTBString();
                namedStreams.Add(streamName);
            }
            if (namedStreamsListEndOffsetExcluded != reader.Offset) {
                throw new PDBFormatException("Named streams list end offset mismatch.");
            }

            // then by an <uint, uint> hash table where key is an index in string buffer and value is a
            // stream index.
            _streamIndexByName = new Dictionary<string, uint>();
            // WARNING : The second parameter is a delegate. We don't actually read an integer before invoking
            // the method/
            HashTableContent<uint> hashTable = HashTableContent<uint>.Create(reader, reader.ReadUInt32);

            // Then by a list of feature codes.
            uint remainingBytes = (pdbStreamSize - reader.Offset);
            if (0 != (remainingBytes % sizeof(PdbFeatureCodes))) {
                throw new PDBFormatException("Invalid PDB stream size.");
            }
            int featureCodesCount = Utils.SafeCastToInt32(remainingBytes / sizeof(PdbFeatureCodes));
            _featureCodes = new PdbFeatureCodes[featureCodesCount];
            for(int index = 0; index < featureCodesCount; index++) {
                PdbFeatureCodes featureCode = (PdbFeatureCodes)reader.ReadUInt32();
                _featureCodes[index] = featureCode;
                switch (featureCode) {
                    case PdbFeatureCodes.MinimalDebugInfo:
                        MinimalDebugInfoEnabled = true;
                        break;
                    case PdbFeatureCodes.NoTypeMerge:
                        NoTypeMergeEnabled = true;
                        break;
                }
            }

            // We should have reached the end of the stream.
            if (pdbStreamSize != reader.Offset) {
                throw new PDBFormatException("End of stream offset mismatch.");
            }

            // Build the dictionary.
            int namedStreamsCount = namedStreamsBufferOffset.Count;
            foreach (KeyValuePair<uint, uint> pair in hashTable.Enumerate()) {
                uint searchedKey = pair.Key;
                int foundIndex = -1;
                for (int index = 0; index < namedStreamsCount; index++) {
                    if (namedStreamsBufferOffset[index] == searchedKey) {
                        foundIndex = index;
                        break;
                    }
                }
                if (0 > foundIndex) {
                    throw new PDBFormatException("Unable to match string index.");
                }
                // Extract name from stringBuffer
                string streamName = namedStreams[foundIndex];
                _streamIndexByName.Add(streamName, pair.Value);
            }
            return;
        }

        /// <summary>Read the block map blocks and retrieve an array of block indexes to be used by the
        /// stream directory.</summary>
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
            _streamBytesSizes = new uint[numStreams];
            for (int streamIndex = 0; streamIndex < numStreams; streamIndex++) {
                uint streamBytesSize = mapReader.ReadUInt32();
                totalReadBytes += sizeof(uint);
                _streamBytesSizes[streamIndex] = streamBytesSize;
                // NOTE : Some streams such as stream #83 in vcruntime140d.amd64.pdb with hash value
                // 4636DD42F408275AEE31944E871539941 have been found to have uint.MaxValue for count.
                // Assume this is an empty stream.
                if ((0 == streamBytesSize) || (uint.MaxValue == streamBytesSize)) {
                    Console.WriteLine($"DBG : Stream #{streamIndex} is empty.");
                    // Make sure the count is registered as 0 even if uint.MaxValue was found.
                    _streamBytesSizes[streamIndex] = 0;
                    continue;
                }
                if (ShouldTraceStreamDirectory) {
                    Console.WriteLine(
                        $"DBG : Stream #{streamIndex} is {_streamBytesSizes[streamIndex]} bytes.");
                }
            }
            for (int streamIndex = 0; streamIndex < numStreams; streamIndex++) {
                List<uint> streamDescriptor = new List<uint>();
                _streamDescriptors.Add(streamDescriptor);
                uint streamSize = _streamBytesSizes[streamIndex];
                uint streamBlocksCount = Ceil(streamSize, _superBlock.BlockSize);
                if (ShouldTraceStreamDirectory) {
                    Console.Write($"DBG : Stream #{streamIndex} ({streamBlocksCount} blocks) : ");
                }
                for (int blockIndex = 0; blockIndex < streamBlocksCount; blockIndex++) {
                    uint blockNumber = mapReader.ReadUInt32();
                    totalReadBytes += sizeof(uint);
                    streamDescriptor.Add(blockNumber);
                    if (_knownInUseBlocks[blockNumber]) {
                        Console.WriteLine(
                            $"WARN : Block #{blockNumber} in stream #{streamIndex} has already been seen in another stream.");
                    }
                    if (FreeBlocksConsistencyDebugEnabled) {
                        Console.WriteLine($"FBC : Block #{blockNumber} in use by stream #{streamIndex}.");
                    }
                    _knownInUseBlocks[blockNumber] = true;
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

        /// <summary>Read <paramref name="length"/> bytes into <paramref name="into"/> buffer, starting
        /// at <paramref name="offset"/> offset in <paramref name="into"/> buffer.
        /// Bytes are read starting at <paramref name="position"/> position from <see cref="_mappedPdbView"/>.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="into"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
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

        internal ulong ReadUInt64(ref uint offset)
        {
            try { return _mappedPdbView.ReadUInt64(offset); }
            finally { offset += sizeof(ulong); }
        }

        internal T Read<T>(long position)
            where T : struct
        {
            T result;
            _mappedPdbView.Read<T>(position, out result);
            return result;
        }

        internal void RegisterUsedBlock(uint blockIndex)
        {
            if (blockIndex >= (uint)_knownInUseBlocks.Length) {
                throw new ArgumentOutOfRangeException(nameof(blockIndex));
            }
            if (FreeBlocksConsistencyDebugEnabled) {
                Console.WriteLine($"FBC : Block #{blockIndex} is in use.");
            }
            _knownInUseBlocks[blockIndex] = true;
        }

        [Flags()]
        public enum TraceFlags
        {
            None = 0,
            StreamDirectoryBlocks = 0x00000001,
            NamedStreamMap = 0x00000002,
            ModulesInformation = 0x00000004,
            FullDecodingDebug = 0x00000008,
            FreeBlocksConsistencyDebug = 0x00000010
        }
    }
}