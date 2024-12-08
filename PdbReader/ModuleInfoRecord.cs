using System.Runtime.InteropServices;

namespace PdbReader
{
    public class ModuleInfoRecord
    {
        internal _ModuleInfoRecord _data;
        // TODO : Make values a SortedList of sections by their relative
        // memory range within the module.
        private Dictionary<ushort, List<SectionContributionEntry>> _contributionsBySectionIndex;

#if DEBUG
        public uint GlobalOffset { get; private set; }
#endif

        public string ModuleName { get; private set; }

        public string ObjectFileName {get; private set; }

        public uint Offset { get; private set; }

        public uint Size { get; private set; }

        public ushort SymbolStreamIndex { get; private set; }

        internal static ModuleInfoRecord Create(PdbStreamReader reader)
        {
            ModuleInfoRecord result = new ModuleInfoRecord();
#if DEBUG
            uint globalStartOffset = reader.GetGlobalOffset().Value;
            result.GlobalOffset = globalStartOffset;
            uint headerSize = _ModuleInfoRecord.Size;
#endif
            result._data = reader.Read<_ModuleInfoRecord>();
            uint maxLength = uint.MaxValue;
            // Some records have trailing NULL bytes before names. Skip them.
            while (0 == reader.PeekByte()) { 
                reader.ReadByte();
            }
            result.ModuleName = reader.ReadNTBString(ref maxLength);
            result.ObjectFileName = reader.ReadNTBString(ref maxLength);
            result.Offset = result._data.SectionContribution.Offset;
            result.Size = result._data.SectionContribution.Size;
            result.SymbolStreamIndex = result._data.ModuleSymStream;
            return result;
        }

        internal void Dump(int moduleIndex, string prefix)
        {
#if DEBUG
            string subPrefix = prefix + "\t    ";
            Console.WriteLine($"{prefix}Module #{moduleIndex} : {this.ModuleName}");
            Console.WriteLine($"{prefix}\tobject file {this.ObjectFileName}");
            Console.WriteLine(
                $"{prefix}\tstream {SymbolStreamIndex}, offset 0x{this.Offset:X8}, size 0x{this.Size:X8}");
            this._data.SectionContribution.Dump(subPrefix);
#endif
        }

        internal List<SectionContributionEntry>? GetSectionContributionsById(ushort identifier)
        {
            List<SectionContributionEntry>? result;
            if (!_contributionsBySectionIndex.TryGetValue(identifier, out result)) {
                throw new BugException($"Unable to find section #{identifier}");
            }
            return result;
        }

        internal void RegisterSection(SectionContributionEntry contribution)
        {
            if (null == contribution) {
                throw new ArgumentNullException(nameof(contribution));
            }
            if (null == _contributionsBySectionIndex) {
                _contributionsBySectionIndex =
                    new Dictionary<ushort, List<SectionContributionEntry>>();
            }
            List<SectionContributionEntry>? contributions;
            if (!_contributionsBySectionIndex.TryGetValue(contribution.Section, out contributions)) {
                contributions = new List<SectionContributionEntry>();
                _contributionsBySectionIndex.Add(contribution.Section, contributions);
            }
            contributions.Add(contribution);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _ModuleInfoRecord
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_ModuleInfoRecord>();
            
            internal uint unused;
            internal SectionContributionEntry._SectionContributionEntry SectionContribution;
            internal _Flags Flags;
            /// <summary>Type Server Index for this module. This is assumed to be related to
            /// /Zi, but as LLVM treats /Zi as /Z7, this field will always be invalid for LLVM
            /// generated PDBs.</summary>
            internal byte TSM;
            /// <summary>The index of the stream that contains symbol information for this
            /// module. This includes CodeView symbol information as well as source and line
            /// information. If this field is -1, then no additional debug info will be present
            /// for this module (for example, this is what happens when you strip private symbols
            /// from a PDB).</summary>
            internal ushort ModuleSymStream;
            /// <summary>The number of bytes of data from the stream identified by
            /// ModuleSymStream that represent CodeView symbol records.</summary>
            internal uint SymByteSize;
            /// <summary>The number of bytes of data from the stream identified by ModuleSymStream
            /// that represent C11-style CodeView line information.</summary>
            internal uint C11ByteSize;
            /// <summary>The number of bytes of data from the stream identified by ModuleSymStream
            /// that represent C13-style CodeView line information. At most one of C11ByteSize and
            /// C13ByteSize will be non-zero. Modern PDBs always use C13 instead of C11.</summary>
            internal uint C13ByteSize;
            /// <summary>The number of source files that contributed to this module during
            /// compilation.</summary>
            internal ushort SourceFileCount;
            internal ushort Padding;
            internal uint Unused2;
            /// <summary>The offset in the names buffer of the primary translation unit used to
            /// build this module. All PDB files observed to date always have this value equal
            /// to 0.</summary>
            internal uint SourceFileNameIndex;
            /// <summary>The offset in the names buffer of the PDB file containing this module’s
            /// symbol information. This has only been observed to be non-zero for the special
            /// * Linker * module.</summary>
            internal uint PdbFilePathNameIndex;
        }

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
            EC = 0x0002
        }
    }
}
