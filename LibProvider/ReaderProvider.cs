
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
        private FileInfo? _backupFile;
        private MemoryMappedFile? _mappedBackupFile;
        private MemoryMappedViewStream? _mappedBackupFileView;
        private bool _readOnly = true;

        public ReaderProvider(FileInfo inputFile)
        {
            _backupFile = Utils.AssertArgumentNotNull(inputFile, nameof(inputFile));
            _mappedBackupFile = MemoryMappedFile.OpenExisting(_backupFile.FullName, MemoryMappedFileRights.Read);
            _mappedBackupFileView = _mappedBackupFile.CreateViewStream(0, _backupFile.Length,
                MemoryMappedFileAccess.Read);
            AssertValidHeaderTag();
        }

        /// <summary>For use by <see cref="WriterProvider"/> when instanciated in memory.</summary>
        protected ReaderProvider()
        {
            _readOnly = false;
        }

        public virtual bool IsFileBacked => (null != _backupFile);

        public bool IsReadOnly => _readOnly;

        /// <summary></summary>
        /// <exception cref="ParsingException"></exception>
        /// <remarks>On return, <see cref="_mappedBackupFileView"/> position is at 8th byte.</remarks>
        private void AssertValidHeaderTag()
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
            return;
        }
    }
}
