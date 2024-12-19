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
        private List<ModuleInfoRecord> _modules;
        private ushort? _newFPODataStreamIndex;
        private ushort? _omapFromSourceMappingStreamIndex;
        private ushort? _omapToSourceMappingStreamIndex;
        private ushort? _originalSectionHeaderDataStreamIndex;
        private readonly Pdb _owner;
        private Dictionary<uint, Dictionary<ushort, SortedMemoryRangeList<MemoryRange>>> _perModuleSectionRanges;
        private ushort? _pdataStreamIndex;
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

        private uint EditAndContinueSubstreamOffset
            => TypeServerMapSubstreamOffset + _header.TypeServerMapSize;

        private uint FileInformationSubstreamOffset
            => SectionMapSubstreamOffset + _header.SectionMapSize;

        private uint ModuleInformationSubstreamOffset
            => DBIStreamHeader.Size;

        private uint OptionalDebugSubstreamOffset
            => EditAndContinueSubstreamOffset + _header.ECSubstreamSize;

        internal Pdb Pdb => _owner;

        internal uint PublicSymbolsStreamIndex => _header.PublicStreamIndex;

        private uint SectionContributionSubstreamOffset
            => ModuleInformationSubstreamOffset + _header.ModInfoSize;

        private uint SectionMapSubstreamOffset
            => SectionContributionSubstreamOffset + _header.SectionContributionSize;

        private uint TypeServerMapSubstreamOffset
            => FileInformationSubstreamOffset + _header.SourceInfoSize;

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

        /// <summary>Make sure modules definition - as well as associated sections - are
        /// loaded, i.e. <see cref="_modules"/> and <see cref="_modulesPerIndex"/> members
        /// are properly initialized and populated.</summary>
        public void EnsureModulesAreLoaded()
        {
            try {
                if (null != _modules) {
                    return;
                }
                _modules = new List<ModuleInfoRecord>();

                // Set stream position at the begining of the module information substream.
                uint newOffset = ModuleInformationSubstreamOffset;
                _reader.Offset = newOffset;

                uint totalSize = _header.ModInfoSize;
                uint endOffsetExcluded = _reader.Offset + totalSize;
                int moduleIndex = 0;
                Console.WriteLine("MODULES ======================");
                // Read each record in turn.
                for (; _reader.Offset < endOffsetExcluded; moduleIndex++) {
                    ModuleInfoRecord scannedModule = ModuleInfoRecord.Create(_reader);
#if DEBUG
                    scannedModule.Dump(moduleIndex, string.Empty);
                    if (ushort.MaxValue != scannedModule.SymbolStreamIndex) {
                        try { new PdbStreamReader(_owner, scannedModule.SymbolStreamIndex); }
                        catch {
                            Console.WriteLine(
                                $"WARN : Invalid symbol stream index {scannedModule.SymbolStreamIndex} on module {moduleIndex}");
                        }
                    }
#endif
                    _modules.Add(scannedModule);
#if DEBUG
                    continue;
#endif
                }
            }
            finally {
#if DEBUG
                Console.WriteLine($"{_modules.Count} modules found.");
#endif
                EnsureSectionMappingIsLoaded();
            }
            return;
        }

        public void EnsureSectionMappingIsLoaded()
        {
            if (null == _modules) {
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

        private void EnsureSectionContributionsAreLoaded()
        {
            EnsureModulesAreLoaded();
            if (null != _perModuleSectionRanges) {
                return;
            }
            Console.WriteLine("SECTIONS ======================");
            // Set stream position at the begining of the section contribution substream.
            uint newOffset = SectionContributionSubstreamOffset;
            _reader.Offset = newOffset;

            SectionContributionSubstreamVersion version =
                (SectionContributionSubstreamVersion)_reader.ReadUInt32();
            uint totalSize = _header.SectionContributionSize;
            uint endOffsetExcluded = _reader.Offset + totalSize;
            // Read each record in turn.
            uint contributionIndex = 0;
            _perModuleSectionRanges =
                new Dictionary<uint, Dictionary<ushort, SortedMemoryRangeList<MemoryRange>>>();
            uint totalRanges = 0;
            for (; _reader.Offset < endOffsetExcluded; contributionIndex++) {
#if DEBUG
                uint globalOffset = _reader.GetGlobalOffset().Value;
#endif
                totalRanges++;
                SectionContributionEntry entry = SectionContributionEntry.Create(this, _reader);
#if DEBUG
                SectionMapEntry mappedSection = entry.GetSection();
#endif
                Dictionary<ushort, SortedMemoryRangeList<MemoryRange>>? moduleRanges;
                if (!_perModuleSectionRanges.TryGetValue(entry.ModuleIndex, out moduleRanges)) {
                    moduleRanges = new Dictionary<ushort, SortedMemoryRangeList<MemoryRange>>();
                    _perModuleSectionRanges.Add(entry.ModuleIndex, moduleRanges);
                }
                if (null == moduleRanges) {
                    throw new BugException();
                }
                ModuleInfoRecord? contributionModule = _FindModuleByIdUnsafe(entry.ModuleIndex);
#if DEBUG
                if (null == contributionModule) {
                    throw new BugException();
                }
#endif
                SortedMemoryRangeList<MemoryRange>? ranges;
                if (!moduleRanges.TryGetValue(entry.Section, out ranges)) {
                    ranges = new SortedMemoryRangeList<MemoryRange>();
                    moduleRanges.Add(entry.Section, ranges);
                }
                MemoryRange thisRange =
                    new MemoryRange(entry.Offset, (entry.Offset + entry.Size - 1));
                // Insert in sorted order.
                ranges.Add(thisRange, thisRange);
#if DEBUG
                continue;
#endif
            }
#if DEBUG
            foreach (uint moduleId in _perModuleSectionRanges.Keys) {
                ModuleInfoRecord? module = FindModuleById(moduleId);
                if (null == module) {
                    throw new BugException($"No module having id {moduleId}");
                }
                Console.WriteLine(
                    $"Module {moduleId} : {(module.ModuleName ?? "UNNAMED")}");
                Dictionary<ushort, SortedMemoryRangeList<MemoryRange>> perSectionRanges =
                    _perModuleSectionRanges[moduleId];
                foreach (ushort sectionId in perSectionRanges.Keys) {
                    Console.WriteLine($"\tSection {sectionId}");
                    List<SectionContributionEntry>? sectionEntries =
                        module.GetSectionContributionsById(sectionId);
                    SortedMemoryRangeList<MemoryRange> ranges =
                        perSectionRanges[sectionId];
                    foreach(MemoryRange range in ranges.Keys) {
                        Console.WriteLine(
                            $"\t\t0x{range._startOffset:X8} - 0x{range._endOffset:X8}");
                    }
                }
            }
#endif
            return;
        }

        internal ModuleInfoRecord? FindModuleById(uint moduleIdentifier)
        {
            EnsureModulesAreLoaded();
            return _FindModuleByIdUnsafe(moduleIdentifier);
        }

        internal ModuleInfoRecord? FindModuleByRVA(uint moduleIndex)
        {
            throw new NotImplementedException();
            //EnsureModulesAreLoaded();
            //return _FindModuleByRVAUnsafe(moduleIndex);
        }

        /// <summary></summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <remarks>The method is deemed unsafe because module are expected to be
        /// already loaded and no check is performed on this.</remarks>
        private ModuleInfoRecord? _FindModuleByIdUnsafe(uint index)
        {
            int trueIndex = Utils.SafeCastToInt32(index);
            if (_modules.Count <= index) {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _modules[trueIndex];
        }

        internal SectionContributionEntry? FindSectionContribution(
            uint relativeVirtualAddress)
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
                List<_FPO_DATA> result = LoadOptionalStream<_FPO_DATA>(_fpoDataStreamIndex, "FPO Data");
            }
            if (null != _exceptionDataStreamIndex) {
                LoadExceptionDataStream();
            }
            if (null != _fixupDataStreamIndex) {
                List<_FIXUP_DATA> result = LoadOptionalStream<_FIXUP_DATA>(_fixupDataStreamIndex,
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
                List<_FPO_DATA> result = LoadOptionalStream<_FPO_DATA>(_newFPODataStreamIndex, "New FPO Data");
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct DBIStreamHeader
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<DBIStreamHeader>();
            /// <summary>Always uint.MaxValue.</summary>
            internal uint Magic;
            /// <summary>this value always appears to be V70, and it is not clear what the other
            /// values are for.</summary>
            internal StreamVersion VersionHeader;
            /// <summary>The number of times the PDB has been written. Equal to the same field
            /// from the PDB Stream header.</summary>
            internal uint Age;
            /// <summary>The index of the Global Symbol Stream, which contains CodeView symbol
            /// records for all global symbols. Actual records are stored in the symbol record
            /// stream, and are referenced from this stream.</summary>
            internal ushort GlobalStreamIndex;
            /// <summary>A bitfield containing values representing the major and minor version
            /// number of the toolchain (e.g. 12.0 for MSVC 2013) used to build the program.
            /// For bit layout <see cref="GetMajorVersion()"/>,  <see cref="GetMinorVersion()"/>
            /// and <see cref="IsNewVersionFormat()"/></summary>
            internal ushort BuildNumber;
            /// <summary>The index of the Public Symbol Stream, which contains CodeView symbol
            /// records for all public symbols. Actual records are stored in the symbol record
            /// stream, and are referenced from this stream.</summary>
            internal ushort PublicStreamIndex;
            /// <summary>The version number of mspdbXXXX.dll used to produce this PDB.</summary>
            internal ushort PdbDllVersion;
            /// <summary>The stream containing all CodeView symbol records used by the program.
            /// This is used for deduplication, so that many different compilands can refer to
            /// the same symbols without having to include the full record content inside of
            /// each module stream.</summary>
            internal ushort SymRecordStream;
            /// <summary>Unknown</summary>
            internal ushort PdbDllRbld;
            /// <summary>The length of the Module Info Substream.</summary>
            internal uint ModInfoSize;
            /// <summary>The length of the Section Contribution Substream.</summary>
            internal uint SectionContributionSize;
            /// <summary>The length of the Section Map Substream.</summary>
            internal uint SectionMapSize;
            /// <summary>The length of the File Info Substream.</summary>
            internal uint SourceInfoSize;
            /// <summary>The length of the Type Server Map Substream.</summary>
            internal uint TypeServerMapSize;
            /// <summary>The index of the MFC type server in the Type Server Map Substream.</summary>
            internal uint MFCTypeServerIndex;
            /// <summary>The length of the Optional Debug Header Stream.</summary>
            internal int OptionalDbgHeaderSize;
            /// <summary>The length of the EC Substream.</summary>
            internal uint ECSubstreamSize;
            /// <summary>A bitfield containing various information about how the program was
            /// built. For bit layout <see cref="HasConflictingTypes()"/>
            /// </summary>
            internal ushort Flags;
            /// <summary>A value from the CV_CPU_TYPE_e enumeration. Common values are 0x8664
            /// (x86-64) and 0x14C (x86).</summary>
            internal CV_CPU_TYPE_e Machine;
            internal uint Padding;

            internal bool IsNewVersionFormat() => (0 != (BuildNumber & 0x8000));

            internal uint GetMajorVersion() => (uint)((BuildNumber & 0x7F00) >> 8);

            internal uint GetMinorVersion() => (uint)(BuildNumber & 0xFF);

            [Flags()]
            internal enum _Flags : ushort
            {
                IncrementallyLinked = 0x0001,
                PrivateSymbolsStripped = 0x0002,
                HasConflictingTypes = 0x0004,
            }

            /// <summary>Note that values are different from the ones in
            /// <see cref="PdbStreamVersion"/></summary>
            internal enum StreamVersion : uint
            {
                VC41 = 930803,
                V50 = 19960307,
                V60 = 19970606,
                V70 = 19990903,
                V110 = 20091201
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct EditAndContinueMappingHeader
        {
            internal const uint SignatureValue = 0xEFFEEFFE;

            // Should be 0xEFFEEFFE
            internal uint Signature;
            internal uint Unkown1;
            internal uint StringPoolBytesSize;
            internal byte Unknown3;
        }

        // TODO : Undocumented structure.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _FIXUP_DATA
        {
            internal _Flags Flags;
            internal uint Unknown1;
            internal uint Unknown2;

            [Flags()]
            public enum _Flags : uint
            {
                /// <summary>Seems that when this flag is set, Unknown2 may be a length, otherwise
                /// Unknown1 &lt Unknown2 and both are close to each other.</summary>
                HasLength = 0x80000000
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _FPO_DATA
        {
            // offset 1st byte of function code
            internal uint ulOffStart;
            // # bytes in function
            internal uint cbProcSize;
            // # bytes in locals/4
            internal uint cdwLocals;
            // # bytes in params/4
            internal ushort cdwParams;
            // # bytes in prolog
            internal byte cbProlog;
            internal _Flags Flags;

            [Flags()]
            public enum _Flags : byte
            {
                NoRegSaved = 0x00,
                OneRegSaved = 0x01,
                TwoRegsSaved = 0x02,
                ThreeRegsSaved = 0x03,
                FourRegsSaved = 0x04,
                FiveRegsSaved = 0x05,
                SixRegsSaved = 0x06,
                SevenRegsSaved = 0x07,
                HasStructuredExceptionHandling = 0x08,
                UseEBP = 0x10,
                Reserverd = 0x20,
                FrameFPO = 0x40,
                FrameTrap = 0x80,
                FrameTSS = 0xC0
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct SectionMapHeader
        {
            // Number of segment descriptors in table
            internal ushort SecCount;
            // Number of logical segment descriptors
            internal ushort SecCountLog;
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
                return (_endOffset < other._startOffset);
            }

            private bool _IsSubRangeOf(MemoryRange other)
            {
                if (null == other) {
                    throw new ArgumentNullException();
                }
                return (_startOffset >= other._startOffset)
                    && (_endOffset <= other._endOffset);
            }

            internal bool Overlap(MemoryRange other)
            {
                return _Overlap(other, true);
            }

            private bool _Overlap(MemoryRange other, bool crossChek)
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
                if (crossChek) {
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
