using System.Runtime.InteropServices;

using PdbReader;

namespace PdbDownloader
{
    public class Downloader
    {
        private const int CertificateAlignment = 4;
        private const int CodeviewNB10 = 0x3031424e; // '01BN'
        private const int CodeviewRSDS = 0x53445352; // 'SDSR'
        private static readonly string DotNetIdentifier =
            "CN=.NET, O=Microsoft Corporation,".ToLower();
        private const int MaxPdbFileNameLength = 4096;
        private static readonly string MicrosoftIdentifier = "CN=Microsoft Windows".ToLower();
        private const string MicrosoftSymbolServer =
            "https://msdl.microsoft.com/download/symbols";
        private const string SymbolCacheRelativePath = @"AppData\Local\Temp\SymbolCache";
        private int _allocationSize;
        private IntPtr _detectorLoadedAddress;
        private IMAGE_DEBUG_DIRECTORY[] _debugDirectories;
        private IMAGE_DATA_DIRECTORY[] _directories;
        private IMAGE_FILE_HEADER _fileHeader;
        // private bool _hasRelocations;
        private bool _isMicrosoftBinary;
        private IMAGE_OPTIONAL_HEADER64 _optionalHeader;
        private RVAReaderWriter _rvaReaderWriter;
        internal IMAGE_SECTION_HEADER[] _sections;

        internal IMAGE_DATA_DIRECTORY[] Directories
            => _directories ?? throw new BugException();

        public Downloader()
        {
            return;
        }

        private static void AlignReaderPosition(BinaryReader reader, int alignment)
        {
            int modulo = SafeCastLongToInt(reader.BaseStream.Position % alignment);
            int delta = (alignment - modulo) % alignment;
            long nextPosition = reader.BaseStream.Position + delta;
            reader.BaseStream.Position = nextPosition;
        }

        /// <summary>Allocate memory for the module to be loaded.</summary>
        /// <param name="reader">A <see cref="BinaryReader"/> on file content. On return the
        /// caller mut assume the original reader position has been lost.</param>
        /// <param name="context">On return contains definition of various module related structures.</param>
        /// <param name="throwOnNon64Bits">Wether we should throw an exception if the
        /// library is not a 64 bits one.</param>
        /// <returns>true if the module has been successfully loaded.</returns>
        private bool AllocateModuleSpace(BinaryReader reader, bool throwOnNon64Bits)
        {
            reader.BaseStream.Position = 0;
            IMAGE_DOS_HEADER dosHeader = FillStructure<IMAGE_DOS_HEADER>(reader);
            reader.BaseStream.Position = dosHeader.e_lfanew;
            uint ntSignature = reader.ReadUInt32();
            _fileHeader = FillStructure<IMAGE_FILE_HEADER>(reader);
            _optionalHeader = FillStructure<IMAGE_OPTIONAL_HEADER64>(reader);
            if (0x020B != _optionalHeader.Magic) {
                if (!throwOnNon64Bits) {
                    return false;
                }
                throw new BugException("PE32+ format expected.");
            }
            // Directories definition are tailed to the Image Optional Header.
           _directories = new IMAGE_DATA_DIRECTORY[_optionalHeader.NumberOfRvaAndSizes];
            // Read directory entries.
            for (int index = 0; index < _directories.Length; index++) {
                _directories[index] = FillStructure<IMAGE_DATA_DIRECTORY>(reader);
            }
            // Read section entries.
            int sectionsCount = _fileHeader.NumberOfSections;
            _sections = new IMAGE_SECTION_HEADER[sectionsCount];
            for (int index = 0; index < sectionsCount; index++) {
                _sections[index] = FillStructure<IMAGE_SECTION_HEADER>(reader);
            }
            // Compute size required for loading every section. 
            uint minimumSize = 0;
            foreach (IMAGE_SECTION_HEADER scannedSection in _sections) {
                minimumSize = Math.Max(minimumSize,
                    scannedSection.virtualAddress + scannedSection.virtualSize - 1);
            }
            long candidateAllocationSize = (long)((1 + ((minimumSize - 1) / Environment.SystemPageSize))
                * Environment.SystemPageSize);
            _allocationSize = SafeCastLongToInt(candidateAllocationSize);
            _detectorLoadedAddress = Marshal.AllocHGlobal(_allocationSize);
            Zeroize(_detectorLoadedAddress, _allocationSize);
            _rvaReaderWriter = new RVAReaderWriter(_detectorLoadedAddress,
                SafeCastIntToUint(_allocationSize), _optionalHeader.ImageBase);
            return true;
        }
        internal static T AssertNotNull<T>(T? candidate)
        {
            return candidate ?? throw new BugException();
        }

        public FileInfo? CachePdb(FileInfo library)
        {
            if (null == library) {
                throw new ArgumentNullException(nameof(library));
            }
            if (!library.Exists) {
                throw new BugException($"File {library.FullName} doesn't exist.");
            }
            BinaryReader? reader;
            try { reader = new BinaryReader(library.OpenRead()); }
            catch (UnauthorizedAccessException) {
                Console.WriteLine($"{library.FullName} file access is denied.");
                return null;
            }
            try {
                if (null == reader) {
                    throw new BugException();
                }
                try {
                    if (!AllocateModuleSpace(reader, false)) {
                        return null;
                    }
                    PseudoLoadSections(reader);
                    // _hasRelocations = LoadRelocations(reader);
                    //_isMicrosoftBinary = CheckMicrosoftSignature(reader);
                    //if (!_isMicrosoftBinary) {
                    //    return null;
                    //}
                    FileInfo? symbolFile = TryCacheSymbols();
                    //if (null != symbolFile) {
                    //    _symbolHandler = new SymbolHandler(symbolFile);
                    //}
                    return symbolFile;
                }
                catch {
                    if (IntPtr.Zero != _detectorLoadedAddress) {
                        Marshal.FreeHGlobal(_detectorLoadedAddress);
                    }
                    throw;
                }
            }
            finally {
                if (null != reader) {
                    reader.Close();
                    reader = null;
                }
            }
        }

        ///// <summary>Load embedded signing certificates.</summary>
        ///// <param name="reader">The PE binary reader.</param>
        ///// <param name="context"></param>
        ///// <exception cref="ApplicationException"></exception>
        ///// <exception cref="NotSupportedException"></exception>
        //private bool CheckMicrosoftSignature(BinaryReader reader)
        //{
        //    IMAGE_DATA_DIRECTORY certificatesDirectory = 
        //        Directories[(int)KnownDirectories.Security];
        //    if (0 == certificatesDirectory.VirtualAddress) {
        //        Console.WriteLine("Unsigned library.");
        //        return false;
        //    }
        //    // WARNING : The certificate directory is NOT mapped into PE memory,
        //    // hence the virtualAddress member actually is a file offset.
        //    uint certificationBaseFileOffset = certificatesDirectory.VirtualAddress;
        //    reader.BaseStream.Position = certificationBaseFileOffset;
        //    while (true) {
        //        AlignReaderPosition(reader, CertificateAlignment);
        //        uint relativeFileOffset = SafeCastLongToUint(
        //            reader.BaseStream.Position - certificationBaseFileOffset);
        //        if (relativeFileOffset >= certificatesDirectory.Size) {
        //            return false;
        //        }
        //        IMAGE_DIRECTORY_ENTRY_SECURITY certificateHeader =
        //            FillStructure<IMAGE_DIRECTORY_ENTRY_SECURITY>(reader);
        //        int rawCertificateLength = SafeCastLongToInt(
        //            certificateHeader.dwLength - Marshal.SizeOf<IMAGE_DIRECTORY_ENTRY_SECURITY>());
        //        byte[] rawData = new byte[rawCertificateLength];
        //        int inputLength = reader.Read(rawData, 0, rawCertificateLength);
        //        if (inputLength != rawCertificateLength) {
        //            throw new BugException();
        //        }
        //        switch(certificateHeader.wCertificateType) {
        //            case IMAGE_DIRECTORY_ENTRY_SECURITY._CertificateType.X509:
        //            case IMAGE_DIRECTORY_ENTRY_SECURITY._CertificateType.PKCS1ModuleSign:
        //                throw new NotSupportedException(
        //                    $"Signature format {certificateHeader.wCertificateType}");
        //            case IMAGE_DIRECTORY_ENTRY_SECURITY._CertificateType.PKCSSignedData:
        //                //ContentInfo contentInfo = new ContentInfo(rawData);
        //                SignedCms signedCms = new SignedCms();
        //                try { signedCms.Decode(rawData); }
        //                catch { return false; }
        //                if (0 >= signedCms.Certificates.Count) {
        //                    // We will return false when every signature has been
        //                    // checked unsuccessfully
        //                    break;
        //                }
        //                string normalizedSubject = signedCms.Certificates[0]
        //                    .Subject
        //                    .ToLower();
        //                // WARNING : Do NOT consider this a signature check.
        //                // This is just a quick trick for our purpose.
        //                if (   normalizedSubject.StartsWith(MicrosoftIdentifier)
        //                    || normalizedSubject.StartsWith(DotNetIdentifier))
        //                {
        //                    return true;
        //                }
        //                break;
        //            default:
        //                throw new BugException();
        //        }
        //    }
        //}

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

        internal static unsafe T FillStructure<T>(BinaryReader reader)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            reader.Read(buffer, 0, buffer.Length);
            fixed(byte* ptr = buffer) {
                return (T?)Marshal.PtrToStructure<T>(new IntPtr(ptr))
                    ?? throw new BugException("Unexpected null value.");
            }
        }

//        /// <summary></summary>
//        /// <param name="reader">On exit, the caller must assume the initial reader position is lost.
//        /// </param>
//        /// <param name="context"></param>
//        /// <exception cref="NotSupportedException"></exception>
//        private bool LoadRelocations(BinaryReader reader)
//        {
//            // Using the well-known relocation section descriptor index, retrieve the section
//            // descriptor ...
//            IMAGE_DATA_DIRECTORY relocationDirectory =
//                Directories[(int)KnownDirectories.Relocations];
//            if (0 == relocationDirectory.VirtualAddress) {
//                Console.WriteLine($"Module without relocations.");
//                return false;
//            }
//            Console.WriteLine($"Relocation @{relocationDirectory.VirtualAddress:X16} : {relocationDirectory.Size} bytes.");
//            IMAGE_SECTION_HEADER relocationSection = _rvaReaderWriter.FindSection(
//                relocationDirectory.VirtualAddress);
//            uint relocationSectionBasePosition = relocationSection.pointerToRawData;
//            // TODO : Consider using the _rvaReaderWriter instead of the raw reader.

//            // ... and set reader position to be on the first relocation byte.
//            reader.BaseStream.Position = relocationSectionBasePosition;
//            // The relocation section is bounded by its size with variable length structures inside.
//            int relocationSectionLength = SafeCastUintToInt(relocationSection.virtualSize);
//            while((reader.BaseStream.Position - relocationSectionBasePosition) < relocationSectionLength) {
//                // Each block must start on a 32 bit (4 bytes) boundary address.
//                if (0 != (reader.BaseStream.Position % 4)) {
//                    throw new BugException();
//                }
//                // Read relocation block header.
//                IMAGE_BASE_RELOCATION scannedRelocation =
//                    FillStructure<IMAGE_BASE_RELOCATION>(reader);
//                int blockEntryCount = ((int)(scannedRelocation.SizeOfBlock - Marshal.SizeOf<IMAGE_BASE_RELOCATION>()))
//                    / Marshal.SizeOf<ushort>();
//#if TRACE_RELOCATIONS
//                Console.WriteLine($"{scannedRelocation.VirtualAddress:X16} size : {scannedRelocation.SizeOfBlock:X4}");
//#endif
//                // Read each relocation entry and perform fixups / add exclusions as
//                // required by relocation entry type.
//                while (0 < blockEntryCount--) {
//                    ushort relocationRawValue = reader.ReadUInt16();
//                    ushort relocationOffset;
//                    RelocationType relocationType =
//                        Fixups.Parse(relocationRawValue, out relocationOffset);
//                    uint relocationRVA = relocationOffset + scannedRelocation.VirtualAddress;
//#if TRACE_RELOCATIONS
//                    Console.WriteLine($"{relocationType} : {relocationOffset:X3} / {relocationRVA:X16}");
//#endif
//                    switch (relocationType) {
//                        case RelocationType.Absolute:
//                            // Used for padding. Ignore.
//                            continue;
//                        case RelocationType.Direct64Bits:
//                            // The relocation applies to an 8 bytes value located at
//                            // relocationRVA
//                            _fixups.AddFixup(relocationRVA, relocationType);
//                            break;
//                        default:
//                            throw new NotSupportedException($"Relocation type : {relocationType}");
//                    }
//                }
//            }
//            // This should stand unless the library is not guenine.
//            if ((reader.BaseStream.Position - relocationSectionBasePosition) != relocationSectionLength) {
//                throw new BugException();
//            }
//            return true;
//        }

        /// <summary>Mimic windows loader when loading sections defined in the
        /// context.</summary>
        /// <param name="reader">The reader to grab data from. On return the
        /// caller must assume initial reader position is lost.</param>
        /// <param name="context"></param>
        /// <exception cref="ApplicationException"></exception>
        private void PseudoLoadSections(BinaryReader reader)
        {
            if (null == _rvaReaderWriter) {
                throw new BugException();
            }
            uint fileAligment = _optionalHeader.FileAlignment;
            uint memoryAligment = _optionalHeader.SectionAlignment;
            int sectionsCount = _sections.Length;
            for (int index = 0; index < sectionsCount; index++) {
                IMAGE_SECTION_HEADER scannedSection = _sections[index];
#if DEBUG
                string scannedSectionName = scannedSection.GetName();
#endif
                // Initialize reader position.
                uint dataPointer = scannedSection.pointerToRawData;
                if (0 != (dataPointer % fileAligment)) {
                    throw new ApplicationException("BUG");
                }
                if (0 == scannedSection.pointerToRawData) {
                    // Uninitialized section. Nothing to do.
                    continue;
                }
                reader.BaseStream.Position = dataPointer;
                uint copiedSize = Math.Min(scannedSection.virtualSize,
                    scannedSection.sizeOfRawData);
                _rvaReaderWriter.Copy(reader, scannedSection.virtualAddress,
                    copiedSize);
                if (copiedSize < scannedSection.virtualSize) {
                    // Case where section is greater than file provided data.
                    // Zeroize remaining unread bytes.
                    int paddingSize = SafeCastUintToInt(scannedSection.virtualSize - copiedSize);
                    IntPtr zeroizeStartAddress = IntPtr.Add(_detectorLoadedAddress,
                        SafeCastUintToInt(scannedSection.virtualAddress));
                    Zeroize(zeroizeStartAddress, paddingSize);
                }
            }
        }

        internal static uint SafeCastIntToUint(int value)
        {
            if (0 > value) {
                throw new OverflowException("BUG");
            }
            return (uint)value;
        }

        internal static int SafeCastLongToInt(long value)
        {
            if ((int.MaxValue < value) || (int.MinValue > value)) {
                throw new OverflowException("BUG");
            }
            return (int)value;
        }

        internal static uint SafeCastLongToUint(long value)
        {
            if ((uint.MaxValue < value) || (uint.MinValue > value)) {
                throw new BugException();
            }
            return (uint)value;
        }

        internal static int SafeCastUintToInt(uint value)
        {
            if (int.MaxValue < value) {
                throw new OverflowException("BUG");
            }
            return (int)value;
        }

        internal static int SafeCastULongToInt(ulong value)
        {
            if (int.MaxValue < value) {
                throw new OverflowException("BUG");
            }
            return (int)(long)value;
        }

        /// <summary>Attempt to load into cache symbols from Microsoft symbol
        /// server matching current module as defined in given context.</summary>
        /// <param name="context"></param>
        /// <returns>A file descriptor for the PDB file available in cache or a
        /// null reference if the file couldn't be found nor downloaded.</returns>
        /// <exception cref="BugException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ApplicationException"></exception>
        internal FileInfo? TryCacheSymbols()
        {
            IMAGE_DATA_DIRECTORY directory = Directories[(int)KnownDirectories.Debug];
            if (0 == directory.Size) {
                return null;
            }
            int entriesCount = SafeCastLongToInt(
                directory.Size / Marshal.SizeOf<IMAGE_DEBUG_DIRECTORY>());
            IntPtr debugDirectoryAddress = IntPtr.Add(_detectorLoadedAddress,
               SafeCastUintToInt(directory.VirtualAddress));
            _debugDirectories = new IMAGE_DEBUG_DIRECTORY[entriesCount];
            DirectoryInfo symbolCacheDirectory = EnsureSymbolCacheDirectory();
            for (int index = 0; index < entriesCount; index++) {
                IMAGE_DEBUG_DIRECTORY? debugDirectory =
                    Marshal.PtrToStructure<IMAGE_DEBUG_DIRECTORY>(debugDirectoryAddress);
                if (null == debugDirectory) {
                    throw new BugException();
                }
                switch (debugDirectory.Type) {
                    case IMAGE_DEBUG_DIRECTORY.DebuggingInformationType.Codeview:
                        IntPtr debugInformationAddress = IntPtr.Add(
                            _detectorLoadedAddress,
                            SafeCastUintToInt(debugDirectory.AddressOfRawData));
                        // Read first DWORD from debug information raw data which should be
                        // a signature.
                        int signature = Marshal.ReadInt32(debugInformationAddress);
                        IntPtr pdbFileNameAddress = IntPtr.Zero;
                        switch(signature) {
                            case CodeviewNB10:
                                throw new NotSupportedException();
                            case CodeviewRSDS:
                                pdbFileNameAddress = IntPtr.Add(debugInformationAddress,
                                    Marshal.SizeOf<PDB70>());
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                        if (IntPtr.Zero == pdbFileNameAddress) {
                            throw new BugException();
                        }
                        int pdbFileNameLength = 0;
                        char[] pdbFileNameContent = new char[MaxPdbFileNameLength];
                        while (true) {
                            char scannedCharacter = (char)Marshal.ReadByte(
                                pdbFileNameAddress, pdbFileNameLength);
                            if (0 == scannedCharacter) {
                                break;
                            }
                            pdbFileNameContent[pdbFileNameLength] = scannedCharacter;
                            if (pdbFileNameContent.Length <= ++pdbFileNameLength) {
                                throw new BugException();
                            }
                        }
                        string pdbFileName = new string(pdbFileNameContent, 0,
                            pdbFileNameLength);
                        PDB70 pdb70 = Marshal.PtrToStructure<PDB70>(debugInformationAddress);
                        Task<FileInfo> pdbFileTask = TryDownloadPdbFile(symbolCacheDirectory,
                            pdb70, pdbFileName);
                        pdbFileTask.Wait();
                        if (pdbFileTask.IsFaulted) {
                            throw new ApplicationException(
                                $"PDB file '{pdbFileName}' retrieval triggered an error.",
                                pdbFileTask.Exception);
                        }
                        FileInfo pdbFile = pdbFileTask.Result;
                        //if (!_pdbFilesByLibraryName.ContainsKey(pdbFileName)) {
                        //    _pdbFilesByLibraryName.Add(pdbFileName, pdbFile);
                        //}
                        return pdbFile;
                    default:
                        break;
                }
                // Prepare next loop.
                debugDirectoryAddress = IntPtr.Add(debugDirectoryAddress,
                    Marshal.SizeOf<IMAGE_DEBUG_DIRECTORY>());
            }
            return null;
        }

        /// <summary>Attempt to load a PDB file from the Microsoft Symbol Server
        /// </summary>
        /// <param name="symbolCacheDirectory">Target directory where result file
        /// will be cached.</param>
        /// <param name="pdb70"></param>
        /// <param name="pdbFileName">Name of the searched file.</param>
        /// <returns></returns>
        /// <exception cref="BugException"></exception>
        private static async Task<FileInfo> TryDownloadPdbFile(DirectoryInfo symbolCacheDirectory,
            PDB70 pdb70, string pdbFileName)
        {
            string symbolServerFormattedGuid =
                pdb70.GetSymbolServerFormattedGuid();
            string simplePdbFileName = new FileInfo(pdbFileName).Name;
            DirectoryInfo targetCacheDirectory = new DirectoryInfo(
                Path.Combine(symbolCacheDirectory.FullName, simplePdbFileName,
                    symbolServerFormattedGuid));
            // Ensure the directory where the PDB file is to be stored exist.
            if (!targetCacheDirectory.Exists) {
                Directory.CreateDirectory(targetCacheDirectory.FullName);
                do {
                    Thread.Sleep(250);
                    targetCacheDirectory.Refresh();
                } while (!targetCacheDirectory.Exists);
            }
            else {
                // The directory already exist. Is the PDB file already loaded ?
                FileInfo[] pdbFiles = targetCacheDirectory.GetFiles(simplePdbFileName);
                switch (pdbFiles.Length) {
                    case 0:
                        break;
                    case 1:
                        Console.WriteLine(
                            $"PDB file {simplePdbFileName} version {symbolServerFormattedGuid} already cached.");
                        // Yes return it.
                        return pdbFiles[0];
                    default:
                        throw new BugException();
                }
            }
            // The directory exist albeit the PDB file is not yet loaded. Try
            // to grab it.
            Console.WriteLine($"Retrieving '{pdbFileName}' symbols.");
            Uri symbolFileUrl = new Uri(
                $"{MicrosoftSymbolServer}/{pdbFileName}/{symbolServerFormattedGuid}{pdb70.Age:X}/{pdbFileName}");
            using (HttpClient httpClient = new HttpClient()) {
                try {
                    HttpResponseMessage response = await httpClient
                        .GetAsync(symbolFileUrl);
                    response.EnsureSuccessStatusCode();
                    using (Stream pdbContent = response.Content.ReadAsStream()) {
                        FileInfo targetFile = new FileInfo(
                            Path.Combine(targetCacheDirectory.FullName, simplePdbFileName));
                        using (FileStream outputStream = File.OpenWrite(targetFile.FullName)) {
                            pdbContent.CopyTo(outputStream);
                        }
                        targetFile.Refresh();
                        Console.WriteLine(
                            $"PDB file {simplePdbFileName} version {symbolServerFormattedGuid} successfully downloaded.");
                        return targetFile;
                    }
                }
                catch (HttpRequestException e) {
                    Console.WriteLine(
                        $"WARN : PDB file {simplePdbFileName} version {symbolServerFormattedGuid} download failed.");
                    Console.WriteLine($"Message : {e.Message}");
                    return null;
                }
            }
        }

        private static unsafe void Zeroize(IntPtr target, int targetSize)
        {
            void* rawPointer = (void*)target;
            while(targetSize > sizeof(long)) {
                *((long*)rawPointer) = 0L;
                targetSize -= sizeof(long);
            }
            while(targetSize > 0) {
                *((byte*)rawPointer) = 0;
                targetSize -= 1;
            }
        }

        /// <summary>PDB version 7.0. Pdb file name immediately follows this
        /// structure.</summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct PDB70
        {
            [FieldOffset(0)]
            internal int CodeviewSignature;
            [FieldOffset(4)]
            internal Guid Signature;
            /// <summary>The following fields are a split of the signature
            /// Guid which are used for symbol server URL building.</summary>
            [FieldOffset(4)]
            internal uint Signature1;
            [FieldOffset(8)]
            internal ushort Signature2;
            [FieldOffset(10)]
            internal ushort Signature3;
            [FieldOffset(12)]
            internal byte Signature4_0;
            [FieldOffset(13)]
            internal byte Signature4_1;
            [FieldOffset(14)]
            internal byte Signature4_2;
            [FieldOffset(15)]
            internal byte Signature4_3;
            [FieldOffset(16)]
            internal byte Signature4_4;
            [FieldOffset(17)]
            internal byte Signature4_5;
            [FieldOffset(18)]
            internal byte Signature4_6;
            [FieldOffset(19)]
            internal byte Signature4_7;

            [FieldOffset(20)]
            internal int Age;

            internal string GetSymbolServerFormattedGuid()
            {
                return $"{Signature1:X8}{Signature2:X4}{Signature3:X4}{Signature4_0:X2}{Signature4_1:X2}{Signature4_2:X2}{Signature4_3:X2}{Signature4_4:X2}{Signature4_5:X2}{Signature4_6:X2}{Signature4_7:X2}";
            }
        }
    }
}
