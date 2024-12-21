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
        internal _Flags Flags;
        /// <summary>A value from the <see cref="_Machine"/> enumeration. Common values are 0x8664
        /// (x86-64) and 0x14C (x86).</summary>
        internal _Machine Machine;
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

        internal enum _Machine : ushort
        {
            UNKNOWN = 0,
            TargetHost = 0x0001,  // Useful for indicating we want to interact with the host and not a WoW guest.
            I386 = 0x014C, // Intel 386.
            R3000 = 0x0162, // MIPS little-endian, 0x160 big-endian
            R4000 = 0x0166, // MIPS little-endian
            R10000 = 0x0168, // MIPS little-endian
            WCEMIPSV2 = 0x0169, // MIPS little-endian WCE v2
            ALPHA = 0x0184, // Alpha_AXP
            SH3 = 0x01A2, // SH3 little-endian
            SH3DSP = 0x01A3,
            SH3E = 0x01A4, // SH3E little-endian
            SH4 = 0x01A6,  // SH4 little-endian
            SH5 = 0x01A8, // SH5
            ARM = 0x01C0, // ARM Little-Endian
            THUMB = 0x01C2, // ARM Thumb/Thumb-2 Little-Endian
            ARMNT = 0x01C4, // ARM Thumb-2 Little-Endian
            AM33 = 0x01D3,
            POWERPC = 0x01F0, // IBM PowerPC Little-Endian
            POWERPCFP = 0x01F1,
            IA64 = 0x0200, // Intel 64
            MIPS16 = 0x0266, // MIPS
            ALPHA64 = 0x0284, // ALPHA64
            MIPSFPU = 0x0366, // MIPS
            MIPSFPU16 = 0x0466,  // MIPS
            AXP64 = ALPHA64,
            TRICORE = 0x0520, // Infineon
            CEF = 0x0CEF,
            EBC = 0x0EBC, // EFI Byte Code
            AMD64 = 0x8664, // AMD64 (K8)
            M32R = 0x9041, // M32R little-endian
            ARM64 = 0xAA64, // ARM64 Little-Endian
            CEE = 0xC0EE
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
