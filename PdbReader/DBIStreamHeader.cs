using PdbReader.Microsoft;
using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader
{
    /// <remarks>https://llvm.org/docs/PDB/DbiStream.html</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DBIStreamHeader
    {
        internal static readonly uint Size = (uint)Marshal.SizeOf<DBIStreamHeader>();
        /// <summary>Always uint.MaxValue.</summary>
        internal uint Magic;
        /// <summary>this value always appears to be V70, and it is not clear what the other
        /// values are for.</summary>
        internal StreamVersion VersionHeader;
        /// <summary>The number of times the PDB has been written. Equal to the same field
        /// from the PDB Stream header.</summary>
        internal uint Age;
        /// <summary>The index of the Global Symbol Stream, which contains CodeView symbol
        /// records for all global symbols. Actual records are stored in the symbol record
        /// stream, and are referenced from this stream.</summary>
        internal ushort GlobalStreamIndex;
        /// <summary>A bitfield containing values representing the major and minor version
        /// number of the toolchain (e.g. 12.0 for MSVC 2013) used to build the program.
        /// For bit layout <see cref="GetMajorVersion()"/>,  <see cref="GetMinorVersion()"/>
        /// and <see cref="IsNewVersionFormat()"/></summary>
        internal ushort BuildNumber;
        /// <summary>The index of the Public Symbol Stream, which contains CodeView symbol
        /// records for all public symbols. Actual records are stored in the symbol record
        /// stream, and are referenced from this stream.</summary>
        internal ushort PublicStreamIndex;
        /// <summary>The version number of mspdbXXXX.dll used to produce this PDB.</summary>
        internal ushort PdbDllVersion;
        /// <summary>The stream containing all CodeView symbol records used by the program.
        /// This is used for deduplication, so that many different compilands can refer to
        /// the same symbols without having to include the full record content inside of
        /// each module stream.</summary>
        internal ushort SymRecordStream;
        /// <summary>Unknown</summary>
        internal ushort PdbDllRbld;
        /// <summary>The length of the Module Info Substream.</summary>
        internal uint ModInfoSize;
        /// <summary>The length of the Section Contribution Substream.</summary>
        internal uint SectionContributionSize;
        /// <summary>The length of the Section Map Substream.</summary>
        internal uint SectionMapSize;
        /// <summary>The length of the File Info Substream.</summary>
        internal uint SourceInfoSize;
        /// <summary>The length of the Type Server Map Substream.</summary>
        internal uint TypeServerMapSize;
        /// <summary>The index of the MFC type server in the Type Server Map Substream.</summary>
        internal uint MFCTypeServerIndex;
        /// <summary>The length of the Optional Debug Header Stream.</summary>
        internal int OptionalDbgHeaderSize;
        /// <summary>The length of the EC Substream.</summary>
        internal uint ECSubstreamSize;
        /// <summary>A bitfield containing various information about how the program was
        /// built. For bit layout <see cref="HasConflictingTypes()"/>
        /// </summary>
        internal ushort Flags;
        /// <summary>A value from the CV_CPU_TYPE_e enumeration. Common values are 0x8664
        /// (x86-64) and 0x14C (x86).</summary>
        internal CV_CPU_TYPE_e Machine;
        internal uint Padding;

        internal void Dump(StreamWriter into, string prefix)
        {
            into.WriteLine($"{prefix}HEADER");
            prefix += "\t";
            StringBuilder builder = new StringBuilder();
            builder.Append(prefix);
            switch(VersionHeader) {
                case StreamVersion.V41:
                    builder.Append("VC4.1");
                    break;
                case StreamVersion.V50:
                    builder.Append("VC5.0");
                    break;
                case StreamVersion.V60:
                    builder.Append("VC6.0");
                    break;
                case StreamVersion.V70:
                    builder.Append("VC7.0");
                    break;
                case StreamVersion.V110:
                    builder.Append("VC11.0");
                    break;
                default:
                    builder.Append($"Unknown version {(uint)VersionHeader}");
                    break;
            }
            builder.AppendLine(
                $" age={Age}, build={BuildNumber}, GSI={GlobalStreamIndex}, PSI={PublicStreamIndex}, SRS={SymRecordStream}, MFCTSI={MFCTypeServerIndex}");
            builder.AppendLine(
                $"{prefix}Sizes : ECS={ECSubstreamSize}, MI={ModInfoSize}, ODH={OptionalDbgHeaderSize}, SC={SectionContributionSize}, " +
                $"SI={SourceInfoSize}, SM={SectionMapSize}, TMS={TypeServerMapSize}");
            builder.AppendLine($"{prefix}Machine={Machine}, DLLversion={PdbDllVersion}, Flags={Flags}");
            into.Write(builder.ToString());
            return;
        }

        internal bool IsNewVersionFormat() => (0 != (BuildNumber & 0x8000));

        internal uint GetMajorVersion() => (uint)((BuildNumber & 0x7F00) >> 8);

        internal uint GetMinorVersion() => (uint)(BuildNumber & 0xFF);

        [Flags()]
        internal enum _Flags : ushort
        {
            IncrementallyLinked =    0x0001,
            PrivateSymbolsStripped = 0x0002,
            HasConflictingTypes =    0x0004,
        }

        /// <summary>Note that values are different from the ones in
        /// <see cref="PdbStreamVersion"/></summary>
        internal enum StreamVersion : uint
        {
            V41 = 0x000E33F3,
            V50 =  0x013091F3,
            V60 =  0x0130BA2E,
            V70 =  0x01310977,
            V110 = 0x01329141
        }
    }
}
