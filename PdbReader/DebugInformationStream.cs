using System.Runtime.InteropServices;
using PdbReader.Microsoft;

namespace PdbReader
{
    /// <summary></summary>
    /// <remarks>Structures and most comments are from :
    /// https://llvm.org/docs/PDB/DbiStream.html</remarks>
    public class DebugInformationStream
    {
        private const uint ThisStreamIndex = 3;

        private DBIStreamHeader _header;
        private ushort? _exceptionDataStreamIndex;
        private ushort? _fixupDataStreamIndex;
        private ushort? _fpoDataStreamIndex;
        private List<SectionMapEntry> _mappedSections;
        private SortedList<uint, ModuleInfoRecord> _modulesById;
        private ushort? _newFPODataStreamIndex;
        private ushort? _omapFromSourceMappingStreamIndex;
        private ushort? _omapToSourceMappingStreamIndex;
        private ushort? _originalSectionHeaderDataStreamIndex;
        private readonly Pdb _owner;
        private SortedList<uint, Dictionary<ushort, SortedMemoryRangeList<MemoryRange>>> _perModuleIndexSectionRanges;
        private ushort? _pdataStreamIndex;
        /// <summary>A stream reader</summary>
        private readonly PdbStreamReader _reader;
        private ushort? _sectionHeaderDataStreamIndex;
        private ushort? _tokenToRIDMappingStreamIndex;
        private ushort? _xdataStreamIndex;

        internal DebugInformationStream(Pdb owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _reader = new PdbStreamReader(owner, 3);
            // Read header.
            _header = _reader.Read<DBIStreamHeader>();
            if (!_header.IsNewVersionFormat()) {
                throw new NotSupportedException("Legacy DBI stream format.");
            }
            _owner.AssertValidStreamNumber(_header.GlobalStreamIndex);
            _owner.AssertValidStreamNumber(_header.PublicStreamIndex);
            // Read module information substream.
            LoadOptionalStreamsIndex();
            uint streamSize = _owner.GetStreamSize(ThisStreamIndex);
            if (_reader.Offset != streamSize) {
                Console.WriteLine(
                    $"WARN : DBI stream size {streamSize}, {streamSize - _reader.Offset} bytes remaining.");
            }
        }

        private uint EditAndContinueSubstreamOffset => TypeServerMapSubstreamOffset + _header.TypeServerMapSize;

        private uint FileInformationSubstreamOffset => SectionMapSubstreamOffset + _header.SectionMapSize;

        /// <summary>Get the index of the global symbols stream.</summary>
        internal uint GlobalSymbolsStreamIndex => _header.GlobalStreamIndex;

        /// <summary>Offset within the stream of the module information substream. Always at a fixed offset
        /// immediately after the stream header.</summary>
        private uint ModuleInformationSubstreamOffset => DBIStreamHeader.Size;

        private uint OptionalDebugSubstreamOffset => EditAndContinueSubstreamOffset + _header.ECSubstreamSize;

        internal Pdb Pdb => _owner;

        internal uint PublicSymbolsStreamIndex => _header.PublicStreamIndex;

        /// <summary>Section contribution substream comes after module information substream, the size
        /// of the later being available int the DBI stream header.</summary>
        private uint SectionContributionSubstreamOffset => ModuleInformationSubstreamOffset + _header.ModInfoSize;

        private uint SectionMapSubstreamOffset => SectionContributionSubstreamOffset + _header.SectionContributionSize;

        private uint TypeServerMapSubstreamOffset => FileInformationSubstreamOffset + _header.SourceInfoSize;

        private int AssertValidSectionIndex(uint candidate)
        {
            if (null == _mappedSections) {
                throw new BugException();
            }
            int trueIndex = Utils.SafeCastToInt32(candidate);
            if (0 == trueIndex) {
                throw new ArgumentOutOfRangeException(nameof(candidate));
            }
            if (_mappedSections.Count < trueIndex) {
                throw new ArgumentOutOfRangeException(nameof(candidate));
            }
            return trueIndex - 1;
        }

        /// <summary>Dump into <paramref name="into"/> a human readable summary of the DBI stream.</summary>
        /// <param name="into"></param>
        internal void Dump(StreamWriter into, string prefix = "")
        {
            _header.Dump(into, prefix);
            into.WriteLine($"{prefix}MODULES");
            string subPrefix = prefix + "\t";
            string sectionPrefix = subPrefix + "\t";
            int moduleIndex = 0;

            EnsureModulesAreLoaded();
            EnsureSectionContributionsAreLoaded();
            EnsureSectionMappingIsLoaded();
            EnsureFileMappingAreLoaded();
            // TODO : Other substreams exist that we could consider loading :
            // Type server map, EC, Optional debug header.

            // TODO : Transform this output into a per section one.
            //foreach (ModuleInfoRecord scannedModule in _modulesById.Values) {
            //    scannedModule.Dump(into, moduleIndex++, subPrefix);
            //    Console.WriteLine($"{prefix}Module #{scannedModule.Index}.");
            //    uint scannedModuleId = scannedModule.Index;
            //    Dictionary<ushort, SortedMemoryRangeList<MemoryRange>> perSectionRanges;
            //    if (!_perModuleIndexSectionRanges.TryGetValue(Utils.SafeCastToUint32(scannedModuleId),
            //        out perSectionRanges))
            //    {
            //        Console.WriteLine($"{sectionPrefix}No contribution found for this module.");
            //        continue;
            //    }
            //    foreach (ushort sectionId in perSectionRanges.Keys) {
            //        Console.WriteLine($"{sectionPrefix}Section {sectionId}");
            //        List<SectionContributionEntry>? sectionEntries =
            //            scannedModule.GetSectionContributionsById(sectionId);
            //        SortedMemoryRangeList<MemoryRange> ranges = perSectionRanges[sectionId];
            //        foreach (MemoryRange range in ranges.Keys) {
            //            Console.WriteLine($"{sectionPrefix}\t0x{range._startOffset:X8} - 0x{range._endOffset:X8}");
            //        }
            //    }
            //}
            return;
        }

        /// <summary>Make sure file info substream is loaded.</summary>
        public void EnsureFileMappingAreLoaded()
        {
            // Set stream position at the begining of the file info substream.
            uint newOffset = FileInformationSubstreamOffset;
            _reader.Offset = newOffset;

            // The number of modules for which source file information is contained within this substream.
            // Should match the corresponding value from the DBI header.
            ushort modulesCount = _reader.ReadUInt16();
            // In theory this is supposed to contain the number of source files for which this substream
            // contains information. But that would present a problem in that the width of this field being
            // 16-bits would prevent one from having more than 64K source files in a program. In early
            // versions of the file format, this seems to have been the case. In order to support more than
            // this, this field of the is simply ignored, and computed dynamically by summing up the values
            // of the ModFileCounts array. In short, this value should be ignored.
            ushort sourceFilesCount = _reader.ReadUInt16();

            // This array is present, but does not appear to be useful.
            ushort[] moduleIndices = new ushort[modulesCount];
            for (int index = 0; index < modulesCount; index++) {
                moduleIndices[index] = _reader.ReadUInt16();
            }
            // An array of NumModules integers, each one containing the number of source files which
            // contribute to the module at the specified index. While each individual module is limited to
            // 64K contributing source files, the union of all modules’ source files may be greater than 64K.
            // The real number of source files is thus computed by summing this array. Note that summing this
            // array does not give the number of unique source files, only the total number of source file
            // contributions to modules.
            ushort[] moduleFileCounts = new ushort[modulesCount];
            uint realSourceFileCount = 0;
            for (int index = 0; index < modulesCount; index++) {
                moduleFileCounts[index] = _reader.ReadUInt16();
                realSourceFileCount += moduleFileCounts[index];
            }
            // NOTE : we have two ushort arrays of the same size. Hence we are guaranteed to be aligned
            // on a uint boundary. No padding is requesterd.

            // An array of NumSourceFiles integers (where NumSourceFiles here refers to the 32-bit value
            // obtained from summing ModFileCountArray), where each integer is an offset into NamesBuffer
            // pointing to a null terminated string.
            uint[] fileNameOffsets = new uint[Utils.SafeCastToInt32(realSourceFileCount)];
            for (int index = 0; index < fileNameOffsets.Length; index++) {
                fileNameOffsets[index] = _reader.ReadUInt32();
            }
            uint nameBufferStartOffset = _reader.Offset;
            // An array of null terminated strings containing the actual source file names.
            List<string> namesBuffer = new List<string>();
            for (int index = 0; index < fileNameOffsets.Length; index++) {
                _reader.Offset = nameBufferStartOffset + fileNameOffsets[index];
                uint maxLength = uint.MaxValue;
                string filename = _reader.ReadNTBString(ref maxLength);
                namesBuffer.Add(filename);
            }
            return;
        }

        /// <summary>Make sure modules definition - as well as associated sections - are loaded,
        /// i.e. <see cref="_modulesById"/> and <see cref="_modulesPerIndex"/> members are properly initialized
        /// and populated.</summary>
        public void EnsureModulesAreLoaded()
        {
            if (null != _modulesById) {
                return;
            }
            try {
                _modulesById = new SortedList<uint, ModuleInfoRecord>();
                // Set stream position at the begining of the module information substream.
                uint newOffset = ModuleInformationSubstreamOffset;
                _reader.Offset = newOffset;

                uint totalSize = _header.ModInfoSize;
                uint endOffsetExcluded = _reader.Offset + totalSize;
                int moduleIndex = 0;
#if DEBUG
                Console.WriteLine("[*] Loading modules");
#endif
                // Read each record in turn.
                for (; _reader.Offset < endOffsetExcluded; moduleIndex++) {
                    if (2 <= moduleIndex) {
                        int i = 1;
                    }
                    ModuleInfoRecord scannedModule = ModuleInfoRecord.Create(_reader,
                        Utils.SafeCastToUint32(moduleIndex));
                    // scannedModule.Dump(Console.Out, moduleIndex, "");
                    if (scannedModule.HasSymbolStream) {
                        try { new PdbStreamReader(_owner, scannedModule.SymbolStreamIndex); }
                        catch {
                            string warningMessage =
                                $"WARN : Invalid symbol stream index {scannedModule.SymbolStreamIndex} on module {moduleIndex}";
                            if (_owner.StrictChecksEnabled) {
                                throw new PDBFormatException(warningMessage);
                            }
                            Console.WriteLine(warningMessage);
                        }
                    }
                    _modulesById.Add(scannedModule.Index, scannedModule);
#if DEBUG
                    continue;
#endif
                }
            }
            finally {
#if DEBUG
                if (null == _modulesById) {
                    Console.WriteLine("Module dictionary initialization failed.");
                }
                else {
                    Console.WriteLine($"{_modulesById.Count} modules found.");
                }
#endif
                EnsureSectionMappingIsLoaded();
            }
            return;
        }

        /// <summary>Ensure the section contribution substream is loaded which begins at offset 0
        /// immediately after the module info substream.</summary>
        /// <exception cref="BugException"></exception>
        private void EnsureSectionContributionsAreLoaded()
        {
            // Required because we will have to search for modules while creating section contributions.
            EnsureModulesAreLoaded();
            if (null != _perModuleIndexSectionRanges) {
                return;
            }
            Console.WriteLine("[*] Loading section contributions.");
            // Set stream position at the begining of the section contribution substream.
            uint newOffset = SectionContributionSubstreamOffset;
            _reader.Offset = newOffset;
            uint totalSize = _header.SectionContributionSize;
            uint endOffsetExcluded = _reader.Offset + totalSize;

            SectionContributionSubstreamVersion version =
                (SectionContributionSubstreamVersion)_reader.ReadUInt32();
            if (_owner.StrictChecksEnabled) {
                switch(version) {
                    case SectionContributionSubstreamVersion.Ver60:
                    case SectionContributionSubstreamVersion.V2:
                        break;
                    default:
                        throw new PDBFormatException($"Unknown section contribution version {version}.");
                }
            }
            // Read each record in turn.
            uint contributionIndex = 0;
            _perModuleIndexSectionRanges =
                new SortedList<uint, Dictionary<ushort, SortedMemoryRangeList<MemoryRange>>>();
            for (; _reader.Offset < endOffsetExcluded; contributionIndex++) {
#if DEBUG
                // For debugging purpose. This variable is not used anywhere.
                uint globalOffset = _reader.GetGlobalOffset().Value;
#endif
                SectionContributionEntry entry = SectionContributionEntry.Create(this, _reader, version);
#if DEBUG
                SectionMapEntry mappedSection = entry.GetSection();
#endif
                Dictionary<ushort, SortedMemoryRangeList<MemoryRange>>? moduleRanges;
                if (!_perModuleIndexSectionRanges.TryGetValue(entry.ModuleIndex, out moduleRanges)) {
                    moduleRanges = new Dictionary<ushort, SortedMemoryRangeList<MemoryRange>>();
                    _perModuleIndexSectionRanges.Add(entry.ModuleIndex, moduleRanges);
                }
                if (null == moduleRanges) {
                    throw new BugException($"No section range found matching module index {entry.ModuleIndex}.");
                }
                ModuleInfoRecord? contributionModule = _FindModuleByIdUnsafe(entry.ModuleIndex);
#if DEBUG
                if (null == contributionModule) {
                    throw new BugException($"No module found having index {entry.ModuleIndex}");
                }
#endif
                SortedMemoryRangeList<MemoryRange>? ranges;
                if (!moduleRanges.TryGetValue(entry.SectionId, out ranges)) {
                    ranges = new SortedMemoryRangeList<MemoryRange>();
                    moduleRanges.Add(entry.SectionId, ranges);
                }
                MemoryRange thisRange = new MemoryRange(entry.Offset, (entry.Offset + entry.Size - 1));
                // Insert in sorted order.
                ranges.Add(thisRange, thisRange);
#if DEBUG
                continue;
#endif
            }
            return;
        }
        
        public void EnsureSectionMappingIsLoaded()
        {
            if (null == _modulesById) {
                throw new BugException();
            }
            if (null != _mappedSections) {
                return;
            }
            // Set stream position at the begining of the section header data substream.
            uint newOffset = SectionMapSubstreamOffset;
            _reader.Offset = newOffset;
            SectionMapHeader header = _reader.Read<SectionMapHeader>();
            if (ushort.MaxValue == header.SecCount) {
                throw new NotSupportedException();
            }
            _mappedSections = new List<SectionMapEntry>();
            for(int index = 0; index < header.SecCount; index++) {
                SectionMapEntry scannedEntry = SectionMapEntry.Create(_reader);
                _mappedSections.Add(scannedEntry);
            }
#if DEBUG
            Console.WriteLine($"{_mappedSections.Count} mapped sections found.");
#endif
            return;
        }

        internal IEnumerable<ModuleInfoRecord> EnumerateModules()
        {
            EnsureModulesAreLoaded();
            foreach (ModuleInfoRecord module in _modulesById.Values) {
                yield return module;
            }
        }
        
        internal ModuleInfoRecord? FindModuleById(uint moduleIdentifier)
        {
            EnsureModulesAreLoaded();
            return _FindModuleByIdUnsafe(moduleIdentifier);
        }

        internal ModuleInfoRecord? FindModuleByRVA(uint moduleIndex)
        {
            EnsureModulesAreLoaded();
            EnsureSectionContributionsAreLoaded();
            foreach (ModuleInfoRecord candidate in _modulesById.Values) {
            }
            // return _FindModuleByRVAUnsafe(moduleIndex);
            throw new NotImplementedException();
        }

        /// <summary></summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <remarks>The method is deemed unsafe because module are expected to be
        /// already loaded and no check is performed on this.</remarks>
        private ModuleInfoRecord? _FindModuleByIdUnsafe(uint index)
        {
            ModuleInfoRecord? result;
            _modulesById.TryGetValue(index, out result);
            return result;
        }

        internal SectionContributionEntry? FindSectionContribution(uint relativeVirtualAddress)
        {
            EnsureSectionContributionsAreLoaded();

            throw new NotImplementedException();
        }

        private ushort? GetOptionalStreamIndex()
        {
            ushort input = _reader.ReadUInt16();
            // Invalid indexes are sometimes (-1) which equals to uint.MaxValue or
            // a 0 value which being one of the fixed stream indexes (Old Directory)
            // is obviously an invalid value.
            return ((0 == input) || (ushort.MaxValue == input)) ? null : input;
        }

        /// <summary>Retrieve a mapped section by its index.</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="BugException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public SectionMapEntry GetSection(uint index)
        {
            int trueIndex = AssertValidSectionIndex(index);
            return _mappedSections[trueIndex];
        }

        private void LoadOptionalStreamsIndex()
        {
            // Set stream position which should be near the end of the DBI stream.
            ulong newOffset = DBIStreamHeader.Size + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize +
                _header.SourceInfoSize + _header.TypeServerMapSize +
                _header.ECSubstreamSize;
            _reader.Offset = Utils.SafeCastToUint32(newOffset);

            // Read optional streams index.
            _fpoDataStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_fpoDataStreamIndex);
            _exceptionDataStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_exceptionDataStreamIndex);
            _fixupDataStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_fixupDataStreamIndex);
            _omapToSourceMappingStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_omapToSourceMappingStreamIndex);
            _omapFromSourceMappingStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_omapFromSourceMappingStreamIndex);
            _sectionHeaderDataStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_sectionHeaderDataStreamIndex);
            _tokenToRIDMappingStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_tokenToRIDMappingStreamIndex);
            _xdataStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_xdataStreamIndex);
            _pdataStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_pdataStreamIndex);
            _newFPODataStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_newFPODataStreamIndex);
            _originalSectionHeaderDataStreamIndex = GetOptionalStreamIndex();
            _owner.AssertValidStreamNumber(_originalSectionHeaderDataStreamIndex);
        }

        public void LoadEditAndContinueMappings()
        {
            // TODO : Stream structure still unclear.
            return;
            //// Set stream position
            //ulong mappingStartOffset = (uint)Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
            //    _header.SectionContributionSize + _header.SectionMapSize +
            //    _header.SourceInfoSize + _header.TypeServerMapSize;
            //_reader.Offset = Pdb.SafeCastToUint32(mappingStartOffset);

            //EditAndContinueMappingHeader header = _reader.Read<EditAndContinueMappingHeader>();
            //if (EditAndContinueMappingHeader.SignatureValue != header.Signature) {
            //    throw new PDBFormatException(
            //        $"Invalid type server mapping signature 0x{header.Signature}");
            //}

            //uint stringIndex = 0;
            //uint stringPoolStartOffset = _reader.Offset;
            //uint remainingPoolBytes = header.StringPoolBytesSize;
            //while (0 < remainingPoolBytes) {
            //    uint stringPoolRelativeOffset = _reader.Offset - stringPoolStartOffset;
            //    Console.WriteLine(
            //        $"At offset global/relative 0x{_reader.GetGlobalOffset().Value:X8} / 0x{(stringPoolRelativeOffset):X8}");
            //    string input = _reader.ReadNTBString(ref remainingPoolBytes);
            //    Console.WriteLine($"\t#{stringIndex++} : {input}");
            //}
            //uint mappingRelativeOffset = _reader.Offset - (uint)mappingStartOffset;
            //throw new NotImplementedException();
        }

        public void LoadFileInformations()
        {
            // Set stream position
            ulong newOffset = (uint)Marshal.SizeOf<DBIStreamHeader>() +
                _header.ModInfoSize + _header.SectionContributionSize +
                _header.SectionMapSize;
            _reader.Offset = Utils.SafeCastToUint32(newOffset);
            IStreamGlobalOffset globalOffset = _reader.GetGlobalOffset();

            // Read stream content.
            ushort modulesCount = _reader.ReadUInt16();
            // In theory this is supposed to contain the number of source files for which
            // this substream contains information. But that would present a problem in
            // that the width of this field being 16-bits would prevent one from having
            // more than 64K source files in a program. In early versions of the file format,
            // this seems to have been the case. In order to support more than this, this
            // field of the is simply ignored, and computed dynamically by summing up the
            // values of the ModFileCounts array (discussed below). In short, this value
            // should be ignored.
            // NOTE : This value will later be adjusted for 64K carry.
            uint sourceFilesCount = _reader.ReadUInt16();
            // This array is present, but does not appear to be useful. Values are in increasing
            // order. Last ones may be equal to sourceFilesCount. This may suggest that the
            // indice for module X is the index of the first participating file for this module.
            // Modules where moduleIndices value equals sourceFilesCount would be modules with
            // no associated file.
            // NOTE : In PDB file, module indices are defined as ushort values. However, the total
            // number of files may be greater than 64K, so we must extend to an uint values array
            // and later adjust for carry.
            uint[] moduleIndices = new uint[modulesCount];
            _reader.ReadArray(moduleIndices, _reader.ReadUInt16AndCastToUInt32);

            // An array of NumModules integers, each one containing the number of source
            // files which contribute to the module at the specified index. While each
            // individual module is limited to 64K contributing source files, the union of
            // all modules’ source files may be greater than 64K. The real number of source
            // files is thus computed by summing this array. Note that summing this array
            // does not give the number of unique source files, only the total number of
            // source file contributions to modules.
            ushort[] moduleFilesCount = new ushort[modulesCount];
            _reader.ReadArray(moduleFilesCount, _reader.ReadUInt16);

            // Perform adjusting for 64K values addition carry as explained above ...
            uint totalValue = 0;
            for(int index = 0; index < modulesCount; index++) {
                moduleIndices[index] += (totalValue & 0xFFFF0000);
                totalValue += moduleFilesCount[index];
            }
            // ... also perform total source file count adjustment.
            sourceFilesCount += (totalValue & 0xFFFF0000);

            // NOTE : modulesIndices and moduleFilesCount arrays should match, that is for
            // module X : modulesIndices[X+1] - moduleIndices[X] == moduleFilesCount[X]
            int upperCheckBound = modulesCount - 1;
            // We should take for granted the file count of the last module.
            uint realFileCount = moduleFilesCount[upperCheckBound];
            for (int checkIndex = 0; checkIndex < upperCheckBound; checkIndex++) {
                realFileCount += moduleFilesCount[checkIndex];
#if DEBUG
                uint expectedFilesCount;
                if (moduleIndices[checkIndex + 1] < moduleIndices[checkIndex]) {
                    throw new PDBFormatException(
                        $"Module #{checkIndex} first file indice is greater than next module's one.");
                }
                expectedFilesCount = moduleIndices[checkIndex + 1] - moduleIndices[checkIndex];
                if (expectedFilesCount != moduleFilesCount[checkIndex]) {
                    throw new PDBFormatException(
                        $"Module #{checkIndex} is expected to have {expectedFilesCount} files. Modules file count value is {moduleFilesCount[checkIndex]}");
                }
#endif
            }
#if DEBUG
            if (uint.MaxValue > realFileCount) {
                if (realFileCount != sourceFilesCount) {
                    throw new PDBFormatException($"File count discrepancy. Computed vs defined : {realFileCount}/{sourceFilesCount} ");
                }
            }
#endif
            // An array of NumSourceFiles integers (where NumSourceFiles here refers to
            // the 32-bit value obtained from summing moduleFilesCount), where each
            // integer is an offset into NamesBuffer pointing to a null terminated string.
            uint[] fileNameOffsets = new uint[realFileCount];
            _reader.ReadArray(fileNameOffsets, _reader.ReadUInt32);
            // An array of null terminated strings containing the actual source file names.
            Dictionary<uint, string> fileNameByIndex = new Dictionary<uint, string>();
            uint filenameBaseOffset = _reader.Offset;
#if DEBUG
            uint maxFilenameOffset = 0;
#endif
            for(int index = 0; index < realFileCount; index++) {
                uint candidateOffset = fileNameOffsets[index];
                if (fileNameByIndex.ContainsKey(candidateOffset)) {
                    // The file name is already known.
                    continue;
                }
#if DEBUG
                if (maxFilenameOffset < candidateOffset) {
                    maxFilenameOffset = candidateOffset;
                }
#endif
                _reader.Offset = filenameBaseOffset + candidateOffset;
                uint maxStringLength = ushort.MaxValue;
                string filename = _reader.ReadNTBString(ref maxStringLength);
                fileNameByIndex.Add(candidateOffset, filename);
#if DEBUG
                Console.WriteLine($"Added #{fileNameByIndex.Count} '{filename}' Offset 0x{candidateOffset:X8}");
#endif
            }

            // Tracing
            if (_owner.ShouldTraceModules) {
                int offsetIndex = 0;
                for(uint moduleIndex = 0; moduleIndex < modulesCount; moduleIndex++) {
#if DEBUG
                    Console.WriteLine($"Module #{moduleIndex}");
#endif
                    int thisModuleFilesCount = moduleFilesCount[moduleIndex];
                    for (int moduleFileIndex = 0;
                        moduleFileIndex < thisModuleFilesCount;
                        moduleFileIndex++)
                    {
                        uint thisFileOffset = fileNameOffsets[offsetIndex++];
                        string? currentFilename;
                        if (!fileNameByIndex.TryGetValue(thisFileOffset, out currentFilename)) {
                            Console.WriteLine($"\tUnmatched file index {thisFileOffset}");
                        }
#if DEBUG
                        else {
                            Console.WriteLine($"\t{currentFilename}");
                        }
#endif
                    }
                }
            }
            return;
        }

//        public void LoadModuleInformations()
//        {
//            // Set stream position
//            int newOffset = Marshal.SizeOf<DBIStreamHeader>();
//            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

//            // Read stream content.
//            uint offset = 0;
//            uint totalSize = _header.ModInfoSize;
//            int moduleIndex = 0;
//            for(; offset < totalSize; moduleIndex++) {
//                ModuleInfoRecord record = ModuleInfoRecord.Create(_reader);
//                //List<byte> stringBytes = new List<byte>();
//                //// Some records have trailing NULL bytes before names. Skip them
//                //byte scannedByte;
//                //do { scannedByte = _reader.ReadByte(); }
//                //while (0 == scannedByte);
//                //while (0 != scannedByte) {
//                //    stringBytes.Add(scannedByte);
//                //    scannedByte = _reader.ReadByte();
//                //}
//                //string moduleName = Encoding.UTF8.GetString(stringBytes.ToArray());
//                //stringBytes.Clear();
//                //while (0 != (scannedByte = _reader.ReadByte())) {
//                //    stringBytes.Add(scannedByte);
//                //}
//                //string objectFileName = Encoding.UTF8.GetString(stringBytes.ToArray());
//                offset = _reader.Offset;
//                if (_owner.ShouldTraceModules) {
//#if DEBUG
//                    Console.WriteLine($"Module #{moduleIndex}: {record.ModuleName}");
//                    if (!string.IsNullOrEmpty(record.ObjectFileName)) {
//                        Console.WriteLine($"\t{record.ObjectFileName}");
//                    }
//#endif
//                }
//            }
//            return;
//        }

        private List<T> LoadOptionalStream<T>(ushort? streamIndex, string streamName)
            where T : struct
        {
            PdbStreamReader reader = new PdbStreamReader(_owner,
                streamIndex ?? throw new ArgumentNullException());
            List<T> result = new List<T>();
            while (reader.Offset < reader.StreamSize) {
                T thisItem = reader.Read<T>();
                result.Add(thisItem);
            }
            if (reader.Offset != reader.StreamSize) {
                throw new PDBFormatException($"Invalid {streamName} stream format.");
            }
            return result;
        }

        private void LoadExceptionDataStream()
        {
            // TODO : Structure of this stream is unclear.
            PdbStreamReader reader = new PdbStreamReader(_owner,
                _exceptionDataStreamIndex ?? throw new ArgumentNullException());
        }

        public void LoadOptionalStreams()
        {
            // Stream indexes have already been loaded during object initialization.
            if (null != _fpoDataStreamIndex) {
                List<FPO_DATA> result = LoadOptionalStream<FPO_DATA>(_fpoDataStreamIndex, "FPO Data");
            }
            if (null != _exceptionDataStreamIndex) {
                LoadExceptionDataStream();
            }
            if (null != _fixupDataStreamIndex) {
                List<FIXUP_DATA> result = LoadOptionalStream<FIXUP_DATA>(_fixupDataStreamIndex,
                    "Fixup Data");
            }
            if (null != _omapToSourceMappingStreamIndex) {
                // TODO : Stream content is undocumented.
                // throw new NotImplementedException();
            }
            if (null != _omapFromSourceMappingStreamIndex) {
                // TODO : Stream content is undocumented.
                // throw new NotImplementedException();
            }
            if (null != _sectionHeaderDataStreamIndex) {
                // Dump of all section headers from the original file.
                List<IMAGE_SECTION_HEADER> result = LoadOptionalStream<IMAGE_SECTION_HEADER>(
                    _sectionHeaderDataStreamIndex, "Section Headers");
            }
            if (null != _tokenToRIDMappingStreamIndex) {
                // TODO : Stream content is undocumented.
                // throw new NotImplementedException();
            }
            if (null != _xdataStreamIndex) {
                // TODO : Should find exact format for this stream that doesn't look like being
                // well documented.

                //// A copy of the .xdata section from the executable.
                //PdbStreamReader streamReader = new PdbStreamReader(_owner, _xdataStreamIndex.Value);
                //List<UnwindData> result = new List<UnwindData>();
                //while (streamReader.Offset < streamReader.StreamSize) {
                //    UnwindData thisItem = UnwindData.Create(streamReader);
                //    result.Add(thisItem);
                //}
                //if (streamReader.Offset != streamReader.StreamSize) {
                //    throw new PDBFormatException($"Invalid Exception Data stream format.");
                //}
            }
            if (null != _pdataStreamIndex) {
                // TODO : Format is poorly or not documented.
                // throw new NotImplementedException();
            }
            if (null != _newFPODataStreamIndex) {
                List<FPO_DATA> result = LoadOptionalStream<FPO_DATA>(_newFPODataStreamIndex, "New FPO Data");
            }
            if (null != _originalSectionHeaderDataStreamIndex) {
                // TODO : Stream content is undocumented.
                // throw new NotImplementedException();
            }
            return;
        }

        private struct UNWIND_INFO
        {
        }

        public void LoadSectionContributions()
        {
            // Set stream position
            ulong newOffset = (uint)Marshal.SizeOf<DBIStreamHeader>() +
                _header.ModInfoSize;
            _reader.Offset = Utils.SafeCastToUint32(newOffset);

            // Read stream content.
            uint streamVersion = _reader.ReadUInt32();
            switch (streamVersion) {
                case 0xF12EBA2D:
                    // Ver60
                    // TODO
                    break;
                case 0xF13151E4:
                    // V2
                    // TODO
                    break;
                default:
                    throw new PDBFormatException(
                        $"Unknown section contribution version {streamVersion}");
            }
            ushort segmentCount = _reader.ReadUInt16();
            ushort logicalSegmentCount = _reader.ReadUInt16();
            SectionMapEntry mapEntry = SectionMapEntry.Create(_reader);
        }

        public void LoadTypeServerMappings()
        {
            if (0 == _header.TypeServerMapSize) {
                Console.WriteLine("Type server mappings not present.");
                return;
            }
            // Set stream position
            ulong newOffset = (uint)Marshal.SizeOf<DBIStreamHeader>() +
                _header.ModInfoSize + _header.SectionContributionSize +
                _header.SectionMapSize + _header.SourceInfoSize;
            _reader.Offset = Utils.SafeCastToUint32(newOffset);
            throw new NotImplementedException();
        }

        private class SortedMemoryRangeList<T> :
            SortedList<MemoryRange, T>
        {
            internal SortedMemoryRangeList()
                : base(MemoryRange.Comparer.Singleton)
            {
            }

            internal T this[int index]
            {
                get
                {
                    if (0 > index) {
                        throw new ArgumentOutOfRangeException();
                    }
                    IList<MemoryRange> keys = base.Keys;
                    if (Keys.Count <= index) {
                        throw new ArgumentOutOfRangeException();
                    }
                    return base[keys[index]];
                }
            }
        }

        /// <summary>A virtual memory range.</summary>
        private class MemoryRange
        {
            internal readonly uint _startOffset;
            internal readonly uint _endOffset;
            // internal List<ContributionRange>? _subRanges;

            internal MemoryRange(uint startOffset, uint endOffset)
            {
                if (_endOffset < _startOffset) {
                    throw new ArgumentException();
                }
                _startOffset = startOffset;
                _endOffset = endOffset;
            }

            internal bool IsGreaterThan(MemoryRange other)
            {
                if (null == other) {
                    throw new ArgumentNullException();
                }
                return (_startOffset > other._endOffset);
            }

            internal bool IsLesserThan(MemoryRange other)
            {
                if (null == other) {
                    throw new ArgumentNullException();
                }
                return (this._endOffset < other._startOffset);
            }

            private bool _IsSubRangeOf(MemoryRange other)
            {
                if (null == other) {
                    throw new ArgumentNullException();
                }
                return (this._startOffset >= other._startOffset)
                    && (this._endOffset <= other._endOffset);
            }

            internal bool Overlap(MemoryRange other)
            {
                return _Overlap(other, true);
            }

            private bool _Overlap(MemoryRange other, bool crossCheck)
            {
                if (null == other) {
                    throw new ArgumentNullException();
                }
                if (   other.IsLesserThan(this)
                    || other.IsGreaterThan(this)
                    || other._IsSubRangeOf(this)
                    || this._IsSubRangeOf(other))
                {
                    return false;
                }
                if (crossCheck) {
                    if (other._Overlap(this, false)) {
                        throw new BugException();
                    }
                }
                return true;
            }

            //internal void RegisterSubRange(ContributionRange subRange)
            //{
            //    if (null == subRange) {
            //        throw new ArgumentNullException();
            //    }
            //    if (!subRange.IsSubRangeOf(this)) {
            //        throw new ArgumentException();
            //    }
            //    if (null == _subRanges) {
            //        _subRanges = new List<ContributionRange>();
            //        _subRanges.Add(subRange);
            //        return;
            //    }
            //    for(int index = 0; index < _subRanges.Count; index++) {
            //        ContributionRange scannedRange = _subRanges[index];
            //        if (subRange.IsSubRangeOf(scannedRange)) {
            //            scannedRange.RegisterSubRange(subRange);
            //            return;
            //        }
            //        if (subRange.IsLesserThan(scannedRange)) {
            //            if (0 < index) {
            //                if (!subRange.IsGreaterThan(_subRanges[index - 1])) {
            //                    throw new BugException();
            //                }
            //            }
            //            _subRanges.Insert(index, subRange);
            //            return;
            //        }
            //        if (subRange.Overlap(scannedRange)) {
            //            throw new BugException();
            //        }
            //        if (!subRange.IsGreaterThan(scannedRange)) {
            //            throw new BugException();
            //        }
            //    }
            //    _subRanges.Add(subRange);
            //}

            internal class Comparer : IComparer<MemoryRange>
            {
                internal static readonly Comparer Singleton = new Comparer();

                private Comparer()
                {
                }

                int IComparer<MemoryRange>.Compare(MemoryRange? x, MemoryRange? y)
                {
                    if (null == x) {
                        throw new ArgumentNullException(nameof(x));
                    }
                    if (null == y) {
                        throw new ArgumentNullException(nameof(y));
                    }
                    if (x.Overlap(y)) {
                        throw new BugException();
                    }
                    if (y.Overlap(x)) {
                        throw new BugException();
                    }
                    if (x.IsLesserThan(y)) {
                        return -1;
                    }
                    if (y.IsLesserThan(x)) {
                        return 1;
                    }
                    throw new BugException();
                }
            }
        }
    }
}
