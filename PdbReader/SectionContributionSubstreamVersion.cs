
namespace PdbReader
{
    /// <remarks>See https://llvm.org/docs/PDB/DbiStream.html#id5</remarks>
    internal enum SectionContributionSubstreamVersion : uint
    {
        Ver60 = 0xEFFE0000 + 19970605,
        V2 = 0xEFFE0000 + 20140516
    }
}
