using System.Runtime.InteropServices;
using System.Text;

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
        private readonly Pdb _owner;
        private readonly PdbStreamReader _reader;
        private ushort? _fpoDataStreamIndex;
        private ushort? _exceptionDataStreamIndex;
        private ushort? _fixupDataStreamIndex;
        private ushort? _omapToSourceMappingStreamIndex;
        private ushort? _omapFromSourceMappingStreamIndex;
        private ushort? _sectionHeaderDataStreamIndex;
        private ushort? _tokenToRIDMappingStreamIndex;
        private ushort? _xdataStreamIndex;
        private ushort? _pdataStreamIndex;
        private ushort? _newFPODataStreamIndex;
        private ushort? _originalSectionHeaderDataStreamIndex;

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

        private ushort? GetOptionalStreamIndex()
        {
            ushort input = _reader.ReadUInt16();
            // Invalid indexes are sometimes (-1) which equals to uint.MaxValue or
            // a 0 value which being one of the fixed stream indexes (Old Directory)
            // is obviously an invalid value.
            return ((0 == input) || (ushort.MaxValue == input)) ? null : input;
        }

        private void LoadOptionalStreamsIndex()
        {
            // Set stream position which should be near the end of the DBI stream.
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize +
                _header.SourceInfoSize + _header.TypeServerMapSize +
                _header.ECSubstreamSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            // Read optional streams index.
            _fpoDataStreamIndex = GetOptionalStreamIndex();
            _exceptionDataStreamIndex = GetOptionalStreamIndex();
            _fixupDataStreamIndex = GetOptionalStreamIndex();
            _omapToSourceMappingStreamIndex = GetOptionalStreamIndex();
            _omapFromSourceMappingStreamIndex = GetOptionalStreamIndex();
            _sectionHeaderDataStreamIndex = GetOptionalStreamIndex();
            _tokenToRIDMappingStreamIndex = GetOptionalStreamIndex();
            _xdataStreamIndex = GetOptionalStreamIndex();
            _pdataStreamIndex = GetOptionalStreamIndex();
            _newFPODataStreamIndex = GetOptionalStreamIndex();
            _originalSectionHeaderDataStreamIndex = GetOptionalStreamIndex();
        }

        public void LoadEditAndContinueMappings()
        {
            // TODO : Stream structure still unclear.
            return;
            // Set stream position
            int mappingStartOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize +
                _header.SourceInfoSize + _header.TypeServerMapSize;
            _reader.Offset = Pdb.SafeCastToUint32(mappingStartOffset);

            EditAndContinueMappingHeader header = _reader.Read<EditAndContinueMappingHeader>();
            if (EditAndContinueMappingHeader.SignatureValue != header.Signature) {
                throw new PDBFormatException(
                    $"Invalid type server mapping signature 0x{header.Signature}");
            }

            uint stringIndex = 0;
            uint stringPoolStartOffset = _reader.Offset;
            uint remainingPoolBytes = header.StringPoolBytesSize;
            while (0 < remainingPoolBytes) {
                uint stringPoolRelativeOffset = _reader.Offset - stringPoolStartOffset;
                Console.WriteLine(
                    $"At offset global/relative 0x{_reader.GetGlobalOffset().Value:X8} / 0x{(stringPoolRelativeOffset):X8}");
                string input = _reader.ReadNTBString(ref remainingPoolBytes);
                Console.WriteLine($"\t#{stringIndex++} : {input}");
            }
            uint mappingRelativeOffset = _reader.Offset - (uint)mappingStartOffset;
            throw new NotImplementedException();
        }

        public void LoadFileInformations()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);
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
                    throw new PDBFormatException($"Module #{checkIndex} is expected to have {expectedFilesCount} files. Modules file count value is {moduleFilesCount[checkIndex]}");
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
                    Console.WriteLine($"Module #{moduleIndex}");
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
                        else {
                            Console.WriteLine($"\t{currentFilename}");
                        }
                    }
                }
            }
            return;
        }

        public void LoadModuleInformations()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>();
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            // Read stream content.
            uint offset = 0;
            int totalSize = _header.ModInfoSize;
            int moduleIndex = 0;
            for(; offset < totalSize; moduleIndex++) {
                ModuleInfoRecord record = _reader.Read<ModuleInfoRecord>();
                List<byte> stringBytes = new List<byte>();
                // Some records have trailing NULL bytes before names. Skip them
                byte scannedByte;
                do { scannedByte = _reader.ReadByte(); }
                while (0 == scannedByte);
                while (0 != scannedByte) {
                    stringBytes.Add(scannedByte);
                    scannedByte = _reader.ReadByte();
                }
                string moduleName = Encoding.UTF8.GetString(stringBytes.ToArray());
                stringBytes.Clear();
                while (0 != (scannedByte = _reader.ReadByte())) {
                    stringBytes.Add(scannedByte);
                }
                string objectFileName = Encoding.UTF8.GetString(stringBytes.ToArray());
                offset = _reader.Offset;
                if (_owner.ShouldTraceModules) {
                    Console.WriteLine($"Module #{moduleIndex}: {moduleName}");
                    if (!string.IsNullOrEmpty(objectFileName)) {
                        Console.WriteLine($"\t{objectFileName}");
                    }
                }
            }
            return;
        }

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
                List<SectionHeader> result = LoadOptionalStream<SectionHeader>(_sectionHeaderDataStreamIndex,
                    "Section Headers");
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
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

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
            SectionMapEntry mapEntry = _reader.Read<SectionMapEntry>();
        }

        public SectionMapEntry[] LoadSectionMappings()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            ushort sectionDescriptorsCount = _reader.ReadUInt16();
            ushort sectionLogicalDescriptorsCount = _reader.ReadUInt16();
            SectionMapEntry[] result = new SectionMapEntry[sectionDescriptorsCount];
            for(int index = 0; index < sectionDescriptorsCount; index++) {
                result[index] = _reader.Read<SectionMapEntry>();
            }
            return result;
        }

        public void LoadTypeServerMappings()
        {
            if (0 == _header.TypeServerMapSize) {
                Console.WriteLine("Type server mappings not present.");
                return;
            }
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize +
                _header.SourceInfoSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            throw new NotImplementedException();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct DBIStreamHeader
        {
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
            internal int ModInfoSize;
            /// <summary>The length of the Section Contribution Substream.</summary>
            internal int SectionContributionSize;
            /// <summary>The length of the Section Map Substream.</summary>
            internal int SectionMapSize;
            /// <summary>The length of the File Info Substream.</summary>
            internal int SourceInfoSize;
            /// <summary>The length of the Type Server Map Substream.</summary>
            internal int TypeServerMapSize;
            /// <summary>The index of the MFC type server in the Type Server Map Substream.</summary>
            internal uint MFCTypeServerIndex;
            /// <summary>The length of the Optional Debug Header Stream.</summary>
            internal int OptionalDbgHeaderSize;
            /// <summary>The length of the EC Substream.</summary>
            internal int ECSubstreamSize;
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
        public struct SectionHeader
        {
            public byte Name0;
            public byte Name1;
            public byte Name2;
            public byte Name3;
            public byte Name4;
            public byte Name5;
            public byte Name6;
            public byte Name7;
            public uint VirtualSize;
            public uint VirtualAddress;
            public uint SIzeOfRawData;
            public uint PointerToRawData;
            public uint PointerToRelocations;
            public uint PointerToLineNumbers;
            public ushort NumberOfRelocations;
            public ushort NumberOfLineNumbers;
            public _Flags Characteristics;

            [Flags()]
            public enum _Flags : uint
            {
                /// <summary>The section should not be padded to the next boundary.
                /// This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES.
                /// This is valid only for object files.</summary>
                NoPadding = 0x00000008,
                /// <summary>The section contains executable code.</summary>
                ContainsCode = 0x00000020,
                /// <summary>The section contains initialized data.</summary>
                InitializedData = 0x00000040,
                /// <summary>The section contains uninitialized data.</summary>
                UninitializedData = 0x00000080,
                /// <summary>The section contains comments or other information.
                /// The.drectve section has this type. This is valid for object files only.</summary>
                LinkerInfo = 0x00000200,
                /// <summary>The section will not become part of the image. This is valid
                /// only for object files.</summary>
                LinkerShouldRemove = 0x00000800,
                /// <summary>The section contains COMDAT data. For more information, see COMDAT
                /// Sections (Object Only). This is valid only for object files.</summary>
                COMDATData = 0x00001000,
                /// <summary>The section contains data referenced through the global pointer (GP).</summary>
                GlobalPointerReferencedData = 0x00008000,
                /// <summary>Align data on a 1-byte boundary. Valid only for object files.</summary>
                AlignTo1Byte = 0x00100000,
                /// <summary>Align data on a 2-byte boundary. Valid only for object files.</summary>
                AlignTo2Bytes = 0x00200000,
                /// <summary>Align data on a 4-byte boundary. Valid only for object files.</summary>
                AlignTo4Bytes = 0x00300000,
                /// <summary>Align data on a 8-byte boundary. Valid only for object files.</summary>
                AlignTo8Bytes = 0x00400000,
                /// <summary>Align data on a 16-byte boundary. Valid only for object files.</summary>
                AlignTo16Bytes = 0x00500000,
                /// <summary>Align data on a 32-byte boundary. Valid only for object files.</summary>
                AlignTo32Bytes = 0x00600000,
                /// <summary>Align data on a 64-byte boundary. Valid only for object files.</summary>
                AlignTo64Bytes = 0x00700000,
                /// <summary>Align data on a 128-byte boundary. Valid only for object files.</summary>
                AlignTo128Bytes = 0x00800000,
                /// <summary>Align data on a 256-byte boundary. Valid only for object files.</summary>
                AlignTo256Bytes = 0x00900000,
                /// <summary>Align data on a 512-byte boundary. Valid only for object files.</summary>
                AlignTo512Bytes = 0x00A00000,
                /// <summary>Align data on a 1024-byte boundary. Valid only for object files.</summary>
                AlignTo1024Bytes = 0x00B00000,
                /// <summary>Align data on a 2048-byte boundary. Valid only for object files.</summary>
                AlignTo2048Bytes = 0x00C00000,
                /// <summary>Align data on a 4096-byte boundary. Valid only for object files.</summary>
                AlignTo4096Bytes = 0x00D00000,
                /// <summary>Align data on a 8192-byte boundary. Valid only for object files.</summary>
                AlignTo8192Bytes = 0x00E00000,
                /// <summary>The section contains extended relocations.</summary>
                HasExtendedRelocations = 0x01000000,
                /// <summary>The section can be discarded as needed.</summary>
                Discardable = 0x02000000,
                /// <summary>The section cannot be cached.</summary>
                NotCacheable = 0x04000000,
                /// <summary>The section is not pageable.</summary>
                NotPageable = 0x08000000,
                /// <summary>The section can be shared in memory.</summary>
                Shareable = 0x10000000,
                /// <summary>The section can be executed as code.</summary>
                Executable = 0x20000000,
                /// <summary>The section can be read.</summary>
                Readable = 0x40000000,
                /// <summary>The section can be written to.</summary>
                Writable = 0x80000000
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SectionMapEntry
        {
            internal _Flags Flags;
            /// <summary>Logical overlay number</summary>
            internal ushort Ovl;
            /// <summary>Group index into descriptor array.</summary>
            internal ushort Group;
            internal ushort Frame;
            /// <summary>Byte index of segment / group name in string table, or 0xFFFF.</summary>
            internal ushort SectionName;
            /// <summary>Byte index of class in string table, or 0xFFFF.</summary>
            internal ushort ClassName;
            /// <summary>Byte offset of the logical segment within physical segment.
            /// If group is set in flags, this is the offset of the group.</summary>
            internal uint Offset;
            /// <summary>Byte count of the segment or group.</summary>
            internal uint SectionLength;

            [Flags()]
            internal enum _Flags : ushort
            {
                Read = 0x0001,
                Write = 0x0002,
                Execute = 0x0004,
                /// <summary>Descriptor describes a 32-bit linear address.</summary>
                AddressIs32Bit = 0x0008,
                /// <summary>Frame represents a selector.</summary>
                IsSelector = 0x0100,
                /// <summary>Frame represents an absolute address.</summary>
                IsAbsoluteAddress = 0x0200,
                /// <summary>If set, descriptor represents a group.</summary>
                IsGroup = 0x0400
            }
        }
    }
}
