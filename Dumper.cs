using System.Collections;
using System.Reflection;

using PdbDownloader;
using PdbReader;

namespace PdbDumper
{
    public static class Dumper
    {
        private static IEnumerable<FileInfo> _allFiles;
        private static bool _enumeratedFilesArePdb;

        public static int Main(string[] args)
        {
            if (!ParseArgs(args)) {
                Usage();
                return 1;
            }
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
                    scannedPdb = new Downloader().CachePdb(scannedFile);
                    if (null == scannedPdb) {
                        Console.WriteLine($"Can't find or load PDB for file {scannedFile}");
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

        private static void LoadDBIStream(Pdb pdb)
        {
            // The stream header has been read at object instanciation time;
            DebugInformationStream stream = pdb.DebugInfoStream;
            stream.LoadModuleInformations();
            stream.LoadSectionContributions();
            DebugInformationStream.SectionMapEntry[] sections = stream.LoadSectionMappings();
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
                    root = new DirectoryInfo(args[1]);
                    if (!root.Exists) {
                        Console.WriteLine($"Input directory '{root.FullName}' doesn't exist.");
                        return false;
                    }
                    _allFiles = WalkDirectory(root, "*.pdb");
                    _enumeratedFilesArePdb = true;
                    break;
                case "-dir":
                    root = new DirectoryInfo(args[1]);
                    if (!root.Exists) {
                        Console.WriteLine($"Input directory '{root.FullName}' doesn't exist.");
                        return false;
                    }
                    _allFiles = WalkDirectory(root, new string[] { ".dll", ".exe", ".sys" });
                    break;
                default:
                    FileInfo singleFile = new FileInfo(args[0]);
                    if (!singleFile.Exists) {
                        Console.WriteLine($"Input file '{singleFile.FullName}' doesn't exist.");
                        return false;
                    }
                    _allFiles = SingleFileEnumerator(singleFile);
                    break;
            }
            return true;
        }

        private static void Usage()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();

            Console.WriteLine($"{thisAssembly.GetName().Name} <pdb file name>");
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
    }
}