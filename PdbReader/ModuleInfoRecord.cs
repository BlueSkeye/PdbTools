using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader
{
    public class ModuleInfoRecord
    {
#if DEBUG
        // For debugging purpose only because we may have a bug to fix on ModuleName property for modules
        // other than the very first one.
        private static bool FirstModule = true;
#endif

        internal _ModuleInfoRecord _data;

        // TODO : Make values a SortedList of sections by their relative memory range within the module.
        // A section may be built from several contributions. Each contribution MUST be disjoint from
        // other ones. However it is unclear whether every section byte must be mapped to a contribution
        // or not.
        private Dictionary<ushort, List<SectionContributionEntry>> _contributionsBySectionIndex =
            new Dictionary<ushort, List<SectionContributionEntry>>();

        private ModuleInfoRecord(PdbStreamReader reader)
        {
            uint maxLength = uint.MaxValue;
#if DEBUG
            uint headerSize = _ModuleInfoRecord.Size;
            GlobalOffset = reader.GetGlobalOffset().Value;
#endif

#if DEBUG
            if (FirstModule) {
                FirstModule = false;
            }
            else {
                uint globalOffset = reader._GetGlobalOffset();
                int i = 1;
            }
#endif
            /// WARNING : _data is the constant sized part of the module info record. It is immediately
            /// followed by two NTB strings for module name and object file name. The caller is responsible for
            /// reading them once <see cref="_ModuleInfoRecord"/> instance is initialized.</summary>
            _data = reader.Read<_ModuleInfoRecord>();

            // Read both strings.
            ModuleName = reader.ReadNTBString(ref maxLength);
            ObjectFileName = reader.ReadNTBString(ref maxLength);
            // WARNING : Due to variable string length, an additional NULL byte may exist that we must skip.
            reader = reader.EnsureAlignment(sizeof(ushort));

            // Extract some key module info key values.
            Offset = _data.SectionContribution.Offset;
            Size = _data.SectionContribution.Size;
            SymbolStreamIndex = _data.ModuleSymStream;
            return;
        }

#if DEBUG
        public uint GlobalOffset { get; private set; }
#endif

        public uint Index { get; private set; }

        public string ModuleName { get; private set; }

        public string ObjectFileName {get; private set; }

        public uint Offset { get; private set; }

        public uint Size { get; private set; }

        public ushort SymbolStreamIndex { get; private set; }

        /// <summary>Create a new <see cref="ModuleInfoRecord"/> from content at current position of the
        /// <paramref name="reader"/></summary>
        /// <param name="reader"></param>
        /// <param name="moduleIndex"></param>
        /// <returns></returns>
        internal static ModuleInfoRecord Create(PdbStreamReader reader, uint moduleIndex)
        {
#if DEBUG
            // For debugging purpose. These fields are unused.
            uint globalStartOffset = reader.GetGlobalOffset().Value;
            uint headerSize = _ModuleInfoRecord.Size;
#endif
            return new ModuleInfoRecord(reader) {
                Index = Utils.SafeCastToUint32(moduleIndex)
            };
        }

        internal void Dump(TextWriter into, int moduleIndex, string prefix)
        {
            into.WriteLine($"{prefix}Module #{moduleIndex} : {this.ModuleName}");
            string subPrefix = prefix + "\t    ";
            into.WriteLine($"{subPrefix}object file {this.ObjectFileName}");
            into.WriteLine(
                $"{subPrefix}stream {SymbolStreamIndex}, offset 0x{this.Offset:X8}, size 0x{this.Size:X8}");
            this._data.SectionContribution.Dump(into, subPrefix);
            into.WriteLine($"{prefix}TSI #{_data.TSM}, module symbols #{_data.ModuleSymStream}:{_data.SymByteSize}:C11={_data.C11ByteSize}:C13={_data.C13ByteSize} bytes");
            into.WriteLine($"{prefix}SFNI #{_data.SourceFileNameIndex}, PNI #{_data.PdbFilePathNameIndex}, {_data.SourceFileCount} files, Flags={_data.Flags}");
        }

        internal List<SectionContributionEntry>? GetSectionContributionsById(ushort identifier)
        {
            List<SectionContributionEntry>? result;
            if (!_contributionsBySectionIndex.TryGetValue(identifier, out result)) {
                throw new BugException($"Unable to find section #{identifier}");
            }
            return result;
        }

        /// <summary>Register the given section as one belonging to this module. A section is always bound
        /// to one and only one module.</summary>
        /// <param name="contribution"></param>
        /// <exception cref="ArgumentNullException"></exception>
        internal void RegisterSection(SectionContributionEntry contribution)
        {
            if (null == contribution) {
                throw new ArgumentNullException(nameof(contribution));
            }
            if (0x00018E0C == contribution.PdbFileOffset) {
                int i = 1;
            }
            if (0x00018E28 == contribution.PdbFileOffset) {
                int i = 1;
            }
            if (null == _contributionsBySectionIndex) {
                // Make sure the section dictionary for this module instance is initialized.
                _contributionsBySectionIndex = new Dictionary<ushort, List<SectionContributionEntry>>();
            }
            List<SectionContributionEntry>? contributions;
            if (!_contributionsBySectionIndex.TryGetValue(contribution.SectionId, out contributions)) {
                contributions = new List<SectionContributionEntry>();
                _contributionsBySectionIndex.Add(contribution.SectionId, contributions);
            }
//            else {
//                StringBuilder msgBuilder = new StringBuilder();
//#if DEBUG
//                msgBuilder.AppendLine($"Duplicated section id #{contribution.SectionId} found.");
//                msgBuilder.AppendLine($"Attempting to register contribution at file offset 0x{contribution.PdbFileOffset:X8}.");
//                msgBuilder.AppendLine($"Already registered from offset 0x{contributions[0].PdbFileOffset:X8}");
//#endif
//                throw new PDBFormatException(msgBuilder.ToString());
//            }
            contributions.Add(contribution);
        }

        /// <summary>A variable sized structure desribing a single module, acting as a module header. Symbol
        /// definitions related to the module are stored in another stream, index of which can be found in
        /// the <see cref="ModuleSymStream"/> member.
        /// WARNING : This structure is the constant sized part of the module info record. It is immediately
        /// followed by two NTB strings for module name and object file name. The caller is responsible for
        /// reading them once this instance is initialized.</summary>
        /// <remarks>See : https://llvm.org/docs/PDB/DbiStream.html#id4</remarks>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _ModuleInfoRecord
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_ModuleInfoRecord>();
            
            internal uint unused;
            /// <summary>Describes the properties of the section in the final binary which contain the code
            /// and data from this module. SectionContr.Characteristics corresponds to the
            /// Characteristics field of the IMAGE_SECTION_HEADER structure.</summary>
            internal SectionContributionEntry._SectionContributionEntry SectionContribution;
            /// <summary>Bitfield value.</summary>
            internal _Flags Flags;
            /// <summary>Type Server Index for this module. This is assumed to be related to /Zi, but as
            /// LLVM treats /Zi as /Z7, this field will always be invalid for LLVM generated PDBs.</summary>
            internal byte TSM;
            /// <summary>The index of the stream that contains symbol information for this module. This
            /// includes CodeView symbol information as well as source and line information. If this field
            /// is -1, then no additional debug info will be present for this module (for example, this is
            /// what happens when you strip private symbols from a PDB).</summary>
            internal ushort ModuleSymStream;
            /// <summary>The number of bytes of data from the stream identified by ModuleSymStream that
            /// represent CodeView symbol records.</summary>
            internal uint SymByteSize;
            /// <summary>The number of bytes of data from the stream identified by ModuleSymStream that
            /// represent C11-style CodeView line information.</summary>
            internal uint C11ByteSize;
            /// <summary>The number of bytes of data from the stream identified by ModuleSymStream that
            /// represent C13-style CodeView line information. At most one of C11ByteSize and C13ByteSize
            /// will be non-zero. Modern PDBs always use C13 instead of C11.</summary>
            internal uint C13ByteSize;
            /// <summary>The number of source files that contributed to this module during compilation.</summary>
            internal ushort SourceFileCount;
            /// <summary>Unknown or unuded</summary>
            internal ushort Padding;
            internal uint Unused2;
            /// <summary>The offset in the names buffer of the primary translation unit used to build this
            /// module. All PDB files observed to date always have this value equal to 0.</summary>
            internal uint SourceFileNameIndex;
            /// <summary>The offset in the names buffer of the PDB file containing this module’s symbol
            /// information. This has only been observed to be non-zero for the special Linker module.</summary>
            internal uint PdbFilePathNameIndex;

            [Flags()]
            public enum _Flags : byte
            {
                // ``true`` if this ModInfo has been written since reading the PDB.  This is
                // likely used to support incremental linking, so that the linker can decide
                // if it needs to commit changes to disk.
                Dirty = 0x0001,
                // ``true`` if EC information is present for this module. EC is presumed to
                // stand for "Edit & Continue", which LLVM does not support.  So this flag
                // will always be be false.
                EC = 0x0002,
            }
        }
    }
}
