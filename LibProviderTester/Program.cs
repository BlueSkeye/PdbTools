using LibProvider;

namespace LibProviderTester
{
    public static class Tester
    {
        private static FileInfo _libraryFile;
        private static Verb _verb;

        private static int DumpLibrary()
        {
            ReaderProvider provider = new ReaderProvider(_libraryFile);
            return 0;
        }
        
        public static int Main(string[] args)
        {
            if (!ParseArgs(args)) {
                Usage();
                return 1;
            }
            switch (_verb) {
                case Verb.DumpLib:
                    return DumpLibrary();
                default:
                    Console.WriteLine($"Unknown verb {_verb.ToString()}");
                    return 2;
            }
        }

        private static bool ParseArgs(string[] args)
        {
            if (0 >= args.Length) {
                Console.WriteLine("No verb provided.");
            }
            string candidateVerb = args[0].ToLower();
            switch (candidateVerb) {
                case "-dump":
                case "/dump":
                    _verb = Verb.DumpLib;
                    if (2 > args.Length) {
                        Console.WriteLine($"Required dumped file name is missing");
                        return false;
                    }
                    _libraryFile = new FileInfo(args[1]);
                    if (!_libraryFile.Exists) {
                        Console.WriteLine($"Input library file {_libraryFile.FullName} doesn't exist.");
                        return false;
                    }
                    return true;
                default:
                    Console.WriteLine($"Unknown verb '{candidateVerb}'");
                    return false;
            }
        }

        private static void Usage()
        {
            Console.WriteLine("TODO must display usage.");
        }

        private enum Verb
        {
            UNDEFINED = 0,
            DumpLib
        }
    }
}
