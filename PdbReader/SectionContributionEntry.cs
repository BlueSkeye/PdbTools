using System.Runtime.InteropServices;

namespace PdbReader
{
    /// <remarks>See https://llvm.org/docs/PDB/DbiStream.html#id4</remarks>
    public class SectionContributionEntry
    {
        private _SectionContributionEntry _data;
        private ModuleInfoRecord _module;
        private DebugInformationStream _owner;
        private readonly uint _pdbFileOffset;

        private SectionContributionEntry(DebugInformationStream owner, _SectionContributionEntry contribution,
            uint contributionPdbFileOffset)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _data = contribution;
            _pdbFileOffset = contributionPdbFileOffset;
            ModuleInfoRecord? ownerModule = owner.FindModuleById(_data.ModuleIndex);
            if (null == ownerModule) {
                throw new BugException();
            }
            _module = ownerModule;
            ownerModule.RegisterSection(this);
            return;
        }

        /// <summary>Get the module owning this section.</summary>
        public ModuleInfoRecord Module => _module;

        /// <summary>Get the identifier of the module owning this section.</summary>
        public uint ModuleIndex => _data.ModuleIndex;

        public uint Offset => _data.Offset;

        /// <summary>For debugging purpose. Returns the offset within the input PDB file of this section
        /// contribution.</summary>
        internal uint PdbFileOffset => _pdbFileOffset;

        /// <summary>Section identifier.</summary>
        public ushort SectionId => _data.SectionIndex;

        public uint Size => _data.Size;

        internal static SectionContributionEntry Create(DebugInformationStream owner, PdbStreamReader reader,
            SectionContributionSubstreamVersion version)
        {
            if (null == reader) {
                throw new ArgumentNullException(nameof(reader));
            }
            uint contributionPdbFileOffset = reader.Offset;
            _SectionContributionEntry contribution = reader.Read<_SectionContributionEntry>();
            if (SectionContributionSubstreamVersion.V2 == version) {
                // Ignored and not documented.
                uint iSectionOffset = reader.ReadUInt32();
            }
            SectionContributionEntry result = new SectionContributionEntry(owner, contribution,
                contributionPdbFileOffset);
            return result;
        }

        internal SectionMapEntry GetSection() => _owner.GetSection(_data.SectionIndex);

        /// <summary>Each section contribution is uniquely identified by the triplet 
        /// Module / Section / Offset-Size. No contribution should overlap with any other one.</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _SectionContributionEntry
        {
            internal static readonly uint StructureSize = (uint)Marshal.SizeOf<ModuleInfoRecord>();

            /// <summary>Section index within the mapped sections array.</summary>
            internal ushort SectionIndex;
            internal ushort _padding1;
            /// <summary>Offset within the relevant section of this contribution.</summary>
            internal uint Offset;
            /// <summary>Size of the contribution.</summary>
            internal uint Size;
            internal uint Characteristics;
            /// <summary>Index of the module this contribution belongs to.</summary>
            internal ushort ModuleIndex;
            internal ushort Padding2;
            /// <summary>Seems to be irrelevant.</summary>
            internal uint DataCrc;
            /// <summary>Seems to be irrelevant.</summary>
            internal uint RelocCrc;

            internal void Dump(TextWriter into, string prefix)
            {
                into.WriteLine($"{prefix}Section #{SectionIndex}, Module #{ModuleIndex}");
                into.WriteLine(
                    $"{prefix}Offset 0x{Offset:X8}, Size 0x{Size:X8}, Characteristics 0x{Characteristics:X8}");
                into.WriteLine($"{prefix}CRC : data 0x{DataCrc:X8}, Reloc 0x{RelocCrc:X8}");
            }

#if DEBUG
            internal void Dump(string prefix)
            {
                Dump(Console.Out, prefix);
            }
        }
#endif
    }
}
