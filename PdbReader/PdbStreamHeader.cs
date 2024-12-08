
namespace PdbReader
{
    internal struct PdbStreamHeader
    {
        internal PdbStreamVersion Version;
        /// <summary>A 32-bit time-stamp generated with a call to time() at the time the
        /// PDB file is written. Note that due to the inherent uniqueness problems of
        /// using a timestamp with 1-second granularity, this field does not really serve
        /// its intended purpose, and as such is typically ignored in favor of the Guid
        /// field, described below.</summary>
        internal uint Signature;
        /// <summary>The number of times the PDB file has been written. This can be used
        /// along with Guid to match the PDB to its corresponding executable.</summary>
        internal uint Age;
        /// <summary>A 128-bit identifier guaranteed to be unique across space and time.
        /// </summary>
        internal Guid UniqueId;
    }
}
