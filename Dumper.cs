using System.Collections;
using System.Reflection;

using PdbReader;

namespace PdbDumper
{
    public static class Dumper
    {
        private static IEnumerable<FileInfo> _allFiles;

        public static int Main(string[] args)
        {
            if (!ParseArgs(args)) {
                Usage();
                return 1;
            }
            try {
                foreach (FileInfo scannedPdb in _allFiles) {
                    Pdb pdb = new Pdb(scannedPdb, Pdb.TraceFlags.None /*StreamDirectoryBlocks*/, true);
                    Console.WriteLine($"INFO : PDB file {scannedPdb.FullName} successfully loaded.");
                    LoadDBIStream(pdb);
                    LoadTPIStream(pdb);
                    LoadIPIStream(pdb);
                    Console.WriteLine($"INFO : PDB file {scannedPdb.FullName} successfully scanned.");
                }
            }
            catch (Exception e) { throw; }
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
        }

        private static void LoadIPIStream(Pdb pdb)
        {
            IdIndexedStream stream = new PdbReader.IdIndexedStream(pdb);
            stream.LoadRecords();
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
            if ("-cached" == args[0]) {
                DirectoryInfo root = new DirectoryInfo(args[1]);
                if (!root.Exists) {
                    Console.WriteLine($"Input direcotry '{root.FullName}' doesn't exist.");
                    return false;
                }
                _allFiles = WalkDirectory(root);
            }
            else {
                FileInfo singleFile = new FileInfo(args[0]);
                if (!singleFile.Exists) {
                    Console.WriteLine($"Input file '{singleFile.FullName}' doesn't exist.");
                    return false;
                }
                _allFiles = SingleFileEnumerator(singleFile);
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

        private static IEnumerable<FileInfo> WalkDirectory(DirectoryInfo root)
        {
            Stack<DirectoryInfo> directoryStack = new Stack<DirectoryInfo>();
            directoryStack.Push(root);
            while (0 < directoryStack.Count) {
                DirectoryInfo currentDirectory = directoryStack.Pop();
                foreach(DirectoryInfo subDirectory in currentDirectory.GetDirectories()) {
                    directoryStack.Push(subDirectory);
                }
                foreach(FileInfo candidateFile in currentDirectory.GetFiles("*.pdb")) {
                    yield return candidateFile;
                }
            }
            yield break;
        }
    }
}