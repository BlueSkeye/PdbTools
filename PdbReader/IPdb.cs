using System.IO;

namespace PdbReader
{
    /// <summary>This interface defines every publicly available method from an object returned by
    /// <see cref="Pdb.Create(FileInfo, Pdb.TraceFlags, bool)"/> method.</summary>
    public interface IPdb
    {
        DebugInformationStream DebugInfoStream { get; }

        /// <summary>Get or set a flag telling whether strict checks are enabled.</summary>
        bool StrictChecksEnabled { get; set; }

        /// <summary>Get index of the string pool.</summary>
        uint StringPoolStreamIndex { get; }

        /// <summary>Dump the hexadecimal content of the DBI stream.</summary>
        /// <param name="outputStream">Output stream where to write dumped content.</param>
        /// <param name="hexadump">true if dumping should in hexadecimal format. Otherwise the content
        /// will be interpreted.</param>
        void DBIDump(StreamWriter outputStream, bool hexadump);

        void DumpPublicSymbols(StreamWriter outputStream);

        void EnsureGlobalStreamIsLoaded();

        /// <summary>Retrieve definition of the module having the given identifier.</param>
        /// <returns>The module definition or a null reference if no such module could be
        /// found.</returns>
        ModuleInfoRecord? FindModuleById(uint identifier);

        /// <summary>Retrieve definition of the module within which the RVA is located.</summary>
        /// <param name="relativeVirtualAddress">The relative virtual address to be searched.</param>
        /// <returns>The module definition or a null reference if no such module could be found.</returns>
        ModuleInfoRecord? FindModuleByRVA(uint relativeVirtualAddress);

        /// <summary></summary>
        /// <param name="relativeVirtualAddress">RVA of the item which section contribution is to be
        /// retrieved.</param>
        /// <returns></returns>
        SectionContributionEntry? FindSectionContribution(uint relativeVirtualAddress);

        /// <summary>Get a list of file names, each of which participate in the module having
        /// the given index.</summary>
        /// <param name="moduleIndex"></param>
        /// <returns></returns>
        List<string> GetModuleFiles(uint moduleIndex);

        /// <summary>Retrieve a mapped section by its index.</summary>
        /// <param name="index">Index of the searched mapped section.</param>
        /// <returns>The section descriptor.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index value doesn't match any mapped section
        /// index.</exception>
        SectionMapEntry GetSection(uint index);

        /// <summary>Initialize symbols map.</summary>
        void InitializeSymbolsMap();
    }
}
