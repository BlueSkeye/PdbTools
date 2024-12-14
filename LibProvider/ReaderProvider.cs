using System.Diagnostics;
using System.IO.MemoryMappedFiles;

namespace LibProvider
{
    /// <summary></summary>
    /// <remarks>The Windows (PE/COFF) variant is based on the SysV/GNU variant. The first entry "/"
    /// has the same layout as the SysV/GNU symbol table. The second entry is another "/", a Microsoft
    /// extension that stores an extended symbol cross-reference table. This one is sorted and uses
    /// little-endian integers.[5][15] The third entry is the optional "//" long name data as in
    /// SysV/GNU.[16]</remarks>
    public class ReaderProvider
    {
        private static readonly byte[] LibHeaderTag = {
            (byte)'!', (byte)'<', (byte)'a', (byte)'r', (byte)'c', (byte)'h', (byte)'>', (byte)0x0A
        };
        private Dictionary<string, List<ImportFileMember>> _archivedFilesByIdentifier =
            new Dictionary<string, List<ImportFileMember>>();
        private FileInfo? _backupFile;
        private int _backupFileLength = 0;
        private string _backupFileMappingName = Guid.NewGuid().ToString();
        private DebugFlags _debugFlags;
        private FirstLinkerMember? _firstMember;
        private MemoryMappedViewStream _inStream;
        private LongNameMember? _longNameMember;
        private MemoryMappedFile? _mappedBackupFile;
        private MemoryMappedViewStream? _mappedBackupFileView;
        private bool _readOnly = true;
        private SecondLinkerMember? _secondMember;

        public ReaderProvider(FileInfo inputFile, DebugFlags debugFlags = DebugFlags.NONE)
        {
            _debugFlags = debugFlags;
            _backupFile = Utils.AssertArgumentNotNull(inputFile, nameof(inputFile));
            _backupFileLength = Utils.SafeCastToInt32(_backupFile.Length);
            _mappedBackupFile = MemoryMappedFile.CreateFromFile(_backupFile.FullName, FileMode.Open,
                _backupFileMappingName, _backupFile.Length, MemoryMappedFileAccess.Read); 
            _mappedBackupFileView = _mappedBackupFile.CreateViewStream(0, _backupFile.Length,
                MemoryMappedFileAccess.Read);
            _inStream = AssertValidHeaderTag();
            BuildFilesDictionary();
        }

        /// <summary>For use by <see cref="WriterProvider"/> when instanciated in memory.</summary>
        protected ReaderProvider()
        {
            _readOnly = false;
        }

        internal FirstLinkerMember FirstMember
        {
            get => Utils.AssertNotNull(_firstMember);
            private set {
                _firstMember = Utils.AssertArgumentNotNull(value, "FirstMember");
            }
        }

        public virtual bool IsFileBacked => (null != _backupFile);

        public bool IsReadOnly => _readOnly;

        internal SecondLinkerMember SecondMember
        {
            get => Utils.AssertNotNull(_secondMember);
            private set {
                _secondMember = Utils.AssertArgumentNotNull(value, "SecondMember");
            }
        }

        /// <summary></summary>
        /// <exception cref="ParsingException"></exception>
        /// <remarks>On return, <see cref="_mappedBackupFileView"/> position is at 8th byte.</remarks>
        private MemoryMappedViewStream AssertValidHeaderTag()
        {
            MemoryMappedViewStream inStream = Utils.AssertNotNull(_mappedBackupFileView);
            inStream.Position = 0;
            if (LibHeaderTag.Length > inStream.Length) {
                throw new ParsingException(
                    $"Backup file {Utils.AssertNotNull(_backupFile).FullName} is too small.");
            }
            int libHeaderLength = LibHeaderTag.Length;
            byte[] candidateHeader = Utils.AllocateBufferAndAssertRead(inStream, libHeaderLength);
            for(int headerIndex = 0; headerIndex < libHeaderLength; headerIndex++) {
                if (LibHeaderTag[headerIndex] != candidateHeader[headerIndex]) {
                    throw new ParsingException($"Header mismatch at offset {headerIndex}.");
                }
            }
            return inStream;
        }

        /// <summary>Populate the <see cref="BuildFilesDictionary"/> dictionary.</summary>
        private void BuildFilesDictionary()
        {
            _firstMember = new FirstLinkerMember(_inStream, DebugFlags.TraceArchiveFileMembers);
            _secondMember = new SecondLinkerMember(_inStream, DebugFlags.TraceArchiveFileMembers);
            string? candidateHeaderName = ArchivedFile.Header.TryGetHeaderName(_inStream);
            if ((null != candidateHeaderName) && ("//" == candidateHeaderName)) {
                _longNameMember = new LongNameMember(_inStream, DebugFlags.TraceArchiveFileMembers);
            }
            while (_backupFileLength > _inStream.Position) {
                long scannedFileStartOffset = _inStream.Position;
                ImportFileMember scannedFile = ImportFileMember.Create(_inStream, _longNameMember, _debugFlags);
                List<ImportFileMember>? homonyms;
                if (!_archivedFilesByIdentifier.TryGetValue(scannedFile.FileHeader.Identifier, out homonyms)) {
                    homonyms = new List<ImportFileMember>();
                    _archivedFilesByIdentifier.Add(scannedFile.FileHeader.Identifier, homonyms);
                }
                homonyms.Add(scannedFile);
            }
            if (_inStream.Length != _inStream.Position) {
                throw new ParsingException($"Archive length mismatch. Length {_inStream.Length}/Position{_inStream.Position}");
            }
            return;
        }

        private bool IsDebugFlagEnabled(DebugFlags scannedFlag)
        {
            return (0 != (_debugFlags & scannedFlag));
        }

        [Flags()]
        public enum DebugFlags : ulong
        {
            NONE = 0x0000000000000000,
            TraceArchiveFileMembers = 0x0000000000000001,
            TraceArchiveFileMemberSectionsOffset = 0x0000000000000002,
            DumpSectionRawData = 0x0000000000000004,
            DumpRelocationData = 0x0000000000000008,
            TraceArchiveFileMemberSectionsData = 0x0000000000000010,
            TraceSymbols = 0x0000000000000020,
        }
    }
}
