using System.IO;

namespace PdbReader
{
    public interface IPdb
    {
        void DBIDump(BinaryWriter outputStream);

        /// <summary>Retrieve definition of the module within which the RVA is located.</summary>
        /// <param name="relativeVirtualAddress">The relative virtual address to be
        /// searched.</param>
        /// <returns>The module definition or a null reference if no such module could be
        /// found.</returns>
        ModuleInfoRecord? FindModuleByRVA(uint relativeVirtualAddress);

        ModuleInfoRecord? FindModuleById(uint identifier);

        SectionContributionEntry? FindSectionContribution(uint relativeVirtualAddress);

        /// <summary>Get a list of file names, each of which participate in the module having
        /// the given index.</summary>
        /// <param name="moduleIndex"></param>
        /// <returns></returns>
        List<string> GetModuleFiles(uint moduleIndex);

        /// <summary>Retrieve a mapped section by its index.</summary>
        /// <param name="index">Index of the searched mapped section.</param>
        /// <returns>The section descriptor.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index value doesn't
        /// match any mapped section index.</exception>
        SectionMapEntry GetSection(uint index);

        void InitializeSymbolsMap();
    }
}
