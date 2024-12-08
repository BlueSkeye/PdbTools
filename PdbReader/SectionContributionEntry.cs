using System.Runtime.InteropServices;

namespace PdbReader
{
    public class SectionContributionEntry
    {
        private DebugInformationStream _owner;
        private _SectionContributionEntry _data;
        private ModuleInfoRecord _module;

        public uint ModuleIndex => _data.ModuleIndex;

        public uint Offset => _data.Offset;

        public ushort Section => _data.SectionIndex;

        public uint Size => _data.Size;
        
        internal static SectionContributionEntry Create(DebugInformationStream owner,
            PdbStreamReader reader)
        {
            if (null == reader) {
                throw new ArgumentNullException(nameof(reader));
            }
            SectionContributionEntry result = new SectionContributionEntry() {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner)),
                _data = reader.Read<_SectionContributionEntry>()
            };
            ModuleInfoRecord? ownerModule = owner.FindModuleByRVA(result._data.ModuleIndex);
            if (null == ownerModule) {
                throw new BugException();
            }
            result._module = ownerModule;
            ownerModule.RegisterSection(result);
            return result;
        }

        internal SectionMapEntry GetSection()
            => _owner.GetSection(_data.SectionIndex);

        /// <summary>Each section contribution is uniquely identified by the triplet 
        /// Module / Section / Offset-Size. No contribution should overlap with any other
        /// one.</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _SectionContributionEntry
        {
            internal static readonly uint StructureSize = (uint)Marshal.SizeOf<ModuleInfoRecord>();

            /// <summary>Section index within the mapped sections array.</summary>
            internal ushort SectionIndex;
            internal ushort Padding1;
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

#if DEBUG
            internal void Dump(string prefix)
            {
                Console.WriteLine($"{prefix}Section #{SectionIndex}, Module #{ModuleIndex}");
                Console.WriteLine($"{prefix}Offset 0x{Offset:X8}, Size 0x{Size:X8}, Characteristics 0x{Characteristics:X8}");
                Console.WriteLine($"{prefix}CRC : data 0x{DataCrc:X8}, Reloc 0x{RelocCrc:X8}");
            }
        }
#endif
    }
}
