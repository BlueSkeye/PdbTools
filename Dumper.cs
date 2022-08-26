using System.Globalization;
using System.Reflection;

using PdbDownloader;
using PdbReader;

namespace PdbDumper
{
    public static class Dumper
    {
        private const string DefaultSymbolCacheRelativePath =
            @"AppData\Local\Temp\SymbolCache";
        private static IEnumerable<FileInfo> _allFiles;
        private static bool _enumeratedFilesArePdb;
        private static uint _explanationRVA;
        private static FileInfo _inputPdb;
        private static FileInfo _outputFile;
        private static DirectoryInfo _rootCacheDirectory;
        private static FileInfo _targetExecutable;
        private static Verb _verb;

        public static int Main(string[] args)
        {
            // Dirty trick to resolve some random discrepancy in assembly loading when
            // debugging under VS 2022
            DirectoryInfo baseDirectory =
                new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
            AppDomain.CurrentDomain.AssemblyResolve +=
                delegate (object? sender, ResolveEventArgs args)
                {
                    AssemblyName failedName = new AssemblyName(args.Name);
                    switch (failedName.Name) {
                        case "PdbReader":
                            return Assembly.LoadFile(Path.Combine(baseDirectory.FullName, "PdbReader.dll"));
                        case "PdbDownloader":
                            return Assembly.LoadFile(Path.Combine(baseDirectory.FullName, "PdbDownloader.dll"));
                        default:
                            return null;
                    }
                };
            if (!ParseArgs(args)) {
                Usage();
                return 1;
            }
            _rootCacheDirectory = new DirectoryInfo(
                Path.Combine(
                    Environment.GetEnvironmentVariable("USERPROFILE")
                        ?? throw new ApplicationException(
                            "Unexpectedly empty USERPROFILE environment variable"),
                    DefaultSymbolCacheRelativePath));
            switch (_verb) {
                case Verb.Enumerate:
                    return EnumerateFiles();
                case Verb.DBIDump:
                    return DBIDump();
                case Verb.Explain:
                    return Explain();
                default:
                    throw new ApplicationException($"BUG : Unknown verb {_verb}");
            }
        }

        private static int DBIDump()
        {
            Pdb.TraceFlags traceFlags =
                0
                // | Pdb.TraceFlags.FullDecodingDebug
                // | Pdb.TraceFlags.StreamDirectoryBlocks
                ;
            Pdb? pdb = Pdb.Create(_inputPdb,  traceFlags, false);
            if (null == pdb) {
                Console.WriteLine($"ERROR : Unable to open PDB.");
                return 1;
            }
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(_outputFile.FullName))) {
                pdb.DBIDump(writer);
                return 0;
            }
        }

        private static int EnumerateFiles()
        {
            Pdb.TraceFlags traceFlags =
                0 
                // | Pdb.TraceFlags.FullDecodingDebug
                // | Pdb.TraceFlags.StreamDirectoryBlocks
                ;

            uint scannedFilesCount = 0;
            foreach (FileInfo scannedFile in _allFiles) {
                FileInfo? scannedPdb;
                if (_enumeratedFilesArePdb) {
                    scannedPdb = scannedFile;
                }
                else {
                    scannedPdb = new Downloader(_rootCacheDirectory).CachePdb(scannedFile);
                    if (null == scannedPdb) {
                        Console.WriteLine(
                            $"Can't find or load PDB for file {scannedFile.FullName}");
                        continue;
                    }
                }
                try {
                    Console.WriteLine($"INFO : Loading PDB file {scannedPdb.FullName}.");
                    Pdb? pdb = Pdb.Create(scannedPdb,  traceFlags, false);
                    if (null == pdb) {
                        Console.WriteLine($"INFO : PDB file won't be scanned.");
                        continue;
                    }
                    Console.WriteLine($"INFO : PDB file {scannedPdb.FullName} successfully loaded.");
                    LoadDBIStream(pdb);
                    LoadTPIStream(pdb);
                    LoadIPIStream(pdb);
                    Console.WriteLine($"INFO : PDB file {scannedPdb.FullName} successfully scanned.");
                    scannedFilesCount++;
                }
                catch (Exception e) { throw; }
            }
            Console.WriteLine($"{scannedFilesCount} files scanned.");
            return 0;
        }

        private static int Explain()
        {
            FileInfo? pdbFile =
                new Downloader(_rootCacheDirectory).CachePdb(_targetExecutable);
            if (null == pdbFile) {
                Console.WriteLine($"Can't find or load PDB for file {_targetExecutable.FullName}");
                return 1;
            }
            Pdb? pdb = Pdb.Create(pdbFile);
            if (null == pdb) {
                throw new ApplicationException($"Couldn't load PDB file {pdbFile.FullName}");
            }
            //ModuleInfoRecord? module = pdb.FindModule(_explanationRVA);
            //if (null == module) {
            //    Console.WriteLine($"Could not find module at RVA 0x{_explanationRVA:X8}");
            //}
            pdb.InitializeSymbolsMap();
            //SectionContributionEntry? contribution = pdb.FindSectionContribution(_explanationRVA);
            //if (null == contribution) {
            //    Console.WriteLine($"Could not find section contribution at RVA 0x{_explanationRVA:X8}");
            //}
            return 0;
        }

        private static void LoadDBIStream(Pdb pdb)
        {
            // The stream header has been read at object instanciation time;
            DebugInformationStream stream = pdb.DebugInfoStream;
            stream.EnsureModulesAreLoaded();
            stream.LoadSectionContributions();
            stream.EnsureSectionMappingIsLoaded();
            stream.LoadFileInformations();
            stream.LoadTypeServerMappings();
            stream.LoadEditAndContinueMappings();
            stream.LoadOptionalStreams();
        }

        private static void LoadIPIStream(Pdb pdb)
        {
            IdIndexedStream stream = PdbReader.IdIndexedStream.Create(pdb);
            if (null == stream) {
                Console.WriteLine("INFO : IPI stream is empty.");
            }
            else {
                stream.LoadRecords();
            }
        }

        private static void LoadTPIStream(Pdb pdb)
        {
            TypeIndexedStream stream = new PdbReader.TypeIndexedStream(pdb);
            stream.LoadRecords();
        }

        private static bool ParseArgs(string[] args)
        {
            if (0 == args.Length) {
                return false;
            }
            DirectoryInfo root;
            switch (args[0].ToLower()) {
                case "-cached":
                    if (1 > args.Length) {
                        Console.WriteLine("Cache directory name is missing.");
                        return false;
                    }
                    root = new DirectoryInfo(args[1]);
                    if (!root.Exists) {
                        Console.WriteLine($"Input directory '{root.FullName}' doesn't exist.");
                        return false;
                    }
                    _allFiles = WalkDirectory(root, "*.pdb");
                    _enumeratedFilesArePdb = true;
                    _verb = Verb.Enumerate;
                    break;
                case "-dbidump":
                    if (1 > args.Length) {
                        Console.WriteLine("Target PDB file name is missing.");
                        return false;
                    }
                    _inputPdb = new FileInfo(args[1]);
                    if (!_inputPdb.Exists) {
                        Console.WriteLine(
                            $"PDB file {_inputPdb.FullName} doesn't exist.");
                        return false;
                    }
                    if (2 > args.Length) {
                        Console.WriteLine("Output file name is missing.");
                        return false;
                    }
                    _outputFile = new FileInfo(args[2]);
                    if (_outputFile.Exists) {
                        Console.WriteLine(
                            $"Output file {_outputFile.FullName} already exist.");
                        return false;
                    }
                    _verb = Verb.DBIDump;
                    return true;
                case "-dir":
                    if (1 > args.Length) {
                        Console.WriteLine("Scanned directory name is missing.");
                        return false;
                    }
                    root = new DirectoryInfo(args[1]);
                    if (!root.Exists) {
                        Console.WriteLine($"Input directory '{root.FullName}' doesn't exist.");
                        return false;
                    }
                    _allFiles = WalkDirectory(root, new string[] { ".dll", ".exe", ".sys" });
                    _verb = Verb.Enumerate;
                    break;
                case "-explain":
                    if (1 > args.Length) {
                        Console.WriteLine("Executable file name is missing.");
                        return false;
                    }
                    _targetExecutable = new FileInfo(args[1]);
                    if (!_targetExecutable.Exists) {
                        Console.WriteLine(
                            $"Executable file {_targetExecutable.FullName} doesn't exist.");
                        return false;
                    }
                    if (2 > args.Length) {
                        Console.WriteLine("Target relative virtual address is missing.");
                        return false;
                    }
                    bool hexadecimal = args[2].StartsWith("0x", StringComparison.OrdinalIgnoreCase);
                    string parsedValue = args[2];
                    if (hexadecimal) {
                        if (2 >= parsedValue.Length) {
                            Console.WriteLine($"Invalid hexadecimal RVA : {args[2]}.");
                            return false;
                        }
                        // Because the 0x prefix is not expected by the parser.
                        parsedValue = parsedValue.Substring(2);
                    }
                    if (!uint.TryParse(parsedValue,
                        (hexadecimal ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer),
                        null, out _explanationRVA))
                    {
                        Console.WriteLine($"Invalid RVA : {args[2]}.");
                        return false;
                    }
                    _verb = Verb.Explain;
                    break;
                default:
                    FileInfo singleFile = new FileInfo(args[0]);
                    if (!singleFile.Exists) {
                        Console.WriteLine($"Input file '{singleFile.FullName}' doesn't exist.");
                        return false;
                    }
                    _allFiles = SingleFileEnumerator(singleFile);
                    _verb = Verb.Enumerate;
                    break;
            }
            return true;
        }

        private static void Usage()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string assemblyName = thisAssembly.GetName().Name;
            Console.WriteLine(
                $"{assemblyName} <pdb file>");
            Console.WriteLine("\t | -cached <directory>");
            Console.WriteLine("\t | -dir <directory>");
            Console.WriteLine("\t | -explain <executable file> <RVA>");
            Console.WriteLine();
            Console.WriteLine("explain : Explain what we can find at relative virtual address");
            Console.WriteLine("\tin the executable file.");
        }

        private static IEnumerable<FileInfo> SingleFileEnumerator(FileInfo candidate)
        {
            yield return candidate;
            yield break;
        }

        private delegate bool FileFilterDelegate(FileInfo candidate);

        private static IEnumerable<FileInfo> WalkDirectory(DirectoryInfo root,
            string fileExtension)
        {
            fileExtension = fileExtension.ToLower();
            return WalkDirectory(root, new string[] { fileExtension });
        }

        private static IEnumerable<FileInfo> WalkDirectory(DirectoryInfo root,
            string[] fileExtensions)
        {
            int extensionsCount = fileExtensions.Length;
            for(int index = 0; index < extensionsCount; index++) {
                fileExtensions[index] = fileExtensions[index].ToLower();
            }
            return WalkDirectory(root, delegate (FileInfo candidate) {
                string candidateName = candidate.Name.ToLower();
                for(int index = 0; index < extensionsCount; index++) {
                    string scannedExtension = fileExtensions[index];
                    if (candidateName.EndsWith(scannedExtension)) {
                        return true;
                    }
                }
                return false;
            });
        }

        private static IEnumerable<FileInfo> WalkDirectory(DirectoryInfo root,
            FileFilterDelegate fileFilter)
        {
            Stack<DirectoryInfo> directoryStack = new Stack<DirectoryInfo>();
            directoryStack.Push(root);
            while (0 < directoryStack.Count) {
                DirectoryInfo currentDirectory = directoryStack.Pop();
                try {
                    foreach(DirectoryInfo subDirectory in currentDirectory.GetDirectories()) {
                        directoryStack.Push(subDirectory);
                    }
                }
                catch (UnauthorizedAccessException uae) {
                    Console.WriteLine($"WARN : Directory {currentDirectory.FullName} ignored (access denied).");
                    continue;
                }
                foreach(FileInfo candidateFile in currentDirectory.GetFiles()) {
                    if ((null == fileFilter) || (fileFilter(candidateFile))) {
                        yield return candidateFile;
                    }
                }
            }
            yield break;
        }

        private enum Verb
        {
            Undefined = 0,
            DBIDump,
            Enumerate,
            Explain
        }
    }
}