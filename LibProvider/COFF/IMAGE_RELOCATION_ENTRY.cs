using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace LibProvider.COFF
{
    internal class IMAGE_RELOCATION_ENTRY
    {
        /// <summary>The address of the item to which relocation is applied. This is the offset from
        /// the beginning of the section, plus the value of the section's RVA/Offset field. See
        /// Section Table (Section Headers). For example, if the first byte of the section has an
        /// address of 0x10, the third byte has an address of 0x12</summary>
        // [FieldOffset(0x00)]
        internal uint VirtualAddress;
        /// <summary>A zero-based index into the symbol table. This symbol gives the address that is
        /// to be used for the relocation. If the specified symbol has section storage class, then
        /// the symbol's address is the address with the first section of the same name.</summary>
        // [FieldOffset(0x04)]
        internal uint SymbolTableIndex;
        /// <summary>A value that indicates the kind of relocation that should be performed. Valid
        /// relocation types depend on machine type.</summary>
        // [FieldOffset(0x08)]
        internal RelocationType Type;

        internal IMAGE_RELOCATION_ENTRY(MemoryMappedViewStream from)
        {
            VirtualAddress = Utils.ReadLittleEndianUInt32(from);
            SymbolTableIndex = Utils.ReadLittleEndianUInt32(from);
            Type = (RelocationType)Utils.ReadLittleEndianUShort(from);
        }

        internal void Dump(string prefix)
        {
            Utils.DebugTrace($"{prefix}0x{VirtualAddress:X8} {SymbolTableIndex} {(RelocationTypeToString(Type))}");
        }

        internal static string RelocationTypeToString(RelocationType candidate)
        {
            switch(candidate) {
                case RelocationType.IMAGE_REL_AMD64_ABSOLUTE:
                    return "ABSOLUTE";
                case RelocationType.IMAGE_REL_AMD64_ADDR64:
                    return "ADDR64";
                case RelocationType.IMAGE_REL_AMD64_ADDR32:
                    return "ADDR32";
                case RelocationType.IMAGE_REL_AMD64_ADDR32NB:
                    return "ADDR32NB";
                case RelocationType.IMAGE_REL_AMD64_REL32:
                    return "REL32"; ;
                case RelocationType.IMAGE_REL_AMD64_REL32_1:
                    return "REL32_1";
                case RelocationType.IMAGE_REL_AMD64_REL32_2:
                    return "REL32_2";
                case RelocationType.IMAGE_REL_AMD64_REL32_3:
                    return "REL32_3";
                case RelocationType.IMAGE_REL_AMD64_REL32_4:
                    return "REL32_4";
                case RelocationType.IMAGE_REL_AMD64_REL32_5:
                    return "REL32_5";
                case RelocationType.IMAGE_REL_AMD64_SECTION:
                    return "SECTION";
                case RelocationType.IMAGE_REL_AMD64_SECREL:
                    return "SECREL";
                case RelocationType.IMAGE_REL_AMD64_SECREL7:
                    return "SECREL7";
                case RelocationType.IMAGE_REL_AMD64_TOKEN:
                    return "TOKEN";
                case RelocationType.IMAGE_REL_AMD64_SREL32:
                    return "SREL32";
                case RelocationType.IMAGE_REL_AMD64_PAIR:
                    return "PAIR";
                case RelocationType.IMAGE_REL_AMD64_SSPAN32:
                    return "SSPAN32";
                default:
                    return "UNKNOWN";
            }
        }

        /// <summary>The following relocation type indicators are defined for x64 and compatible
        /// processors.</summary>
        internal enum RelocationType : ushort
        {
            /// <summary>The relocation is ignored.</summary>
            IMAGE_REL_AMD64_ABSOLUTE = 0,
            /// <summary>The 64-bit VA of the relocation target.</summary>
            IMAGE_REL_AMD64_ADDR64 = 1,
            /// <summary>The 32-bit VA of the relocation target.</summary>
            IMAGE_REL_AMD64_ADDR32 = 2,
            /// <summary>The 32-bit address without an image base (RVA).</summary>
            IMAGE_REL_AMD64_ADDR32NB = 3,
            /// <summary>The 32-bit relative address from the byte following the relocation.</summary>
            IMAGE_REL_AMD64_REL32 = 4,
            /// <summary>The 32-bit address relative to byte distance 1 from the relocation.</summary>
            IMAGE_REL_AMD64_REL32_1 = 5,
            /// <summary>The 32-bit address relative to byte distance 2 from the relocation.</summary>
            IMAGE_REL_AMD64_REL32_2 = 6,
            /// <summary>The 32-bit address relative to byte distance 3 from the relocation.</summary>
            IMAGE_REL_AMD64_REL32_3 = 7,
            /// <summary>The 32-bit address relative to byte distance 4 from the relocation.</summary>
            IMAGE_REL_AMD64_REL32_4 = 8,
            /// <summary>The 32-bit address relative to byte distance 5 from the relocation.</summary>
            IMAGE_REL_AMD64_REL32_5 = 9,
            /// <summary>The 16-bit section index of the section that contains the target. This is used
            /// to support debugging information.</summary>
            IMAGE_REL_AMD64_SECTION = 10,
            /// <summary>The 32-bit offset of the target from the beginning of its section. This is used
            /// to support debugging information and static thread local storage.</summary>
            IMAGE_REL_AMD64_SECREL = 11,
            /// <summary>A 7-bit unsigned offset from the base of the section that contains the target.</summary>
            IMAGE_REL_AMD64_SECREL7 = 12,
            /// <summary>CLR tokens.</summary>
            IMAGE_REL_AMD64_TOKEN = 13,
            /// <summary>A 32-bit signed span-dependent value emitted into the object.</summary>
            IMAGE_REL_AMD64_SREL32 = 14,
            /// <summary>A pair that must immediately follow every span-dependent value.</summary>
            IMAGE_REL_AMD64_PAIR = 15,
            /// <summary>A 32-bit signed span-dependent value that is applied at link time.</summary>
            IMAGE_REL_AMD64_SSPAN32 = 16,
        }
    }
}
