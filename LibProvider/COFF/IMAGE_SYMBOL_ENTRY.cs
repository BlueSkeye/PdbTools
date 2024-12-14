using Microsoft.VisualBasic;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;

namespace LibProvider.COFF
{
    internal class IMAGE_SYMBOL_ENTRY
    {
        /// <summary>The symbol has an absolute (non-relocatable) value and is not an address.</summary>
        internal const short AbsoluteSymbol = -1;
        /// <summary>The symbol provides general type or debugging information but does not correspond to
        /// a section. Microsoft tools use this setting along with .file records (storage class FILE).</summary>
        internal const short DebugSymbol = -2;
        internal const int InFileEntrySize = 18;
        /// <summary>The symbol record is not yet assigned a section. A value of zero indicates that a
        /// reference to an external symbol is defined elsewhere. A value of non-zero is a common symbol
        /// with a size that is specified by the value.</summary>
        internal const short UndefinedSection = 0;
        internal readonly string Name; // 8 bytes
        internal readonly uint Value;
        internal readonly short SectionNumber;
        internal readonly ushort SymbolType;
        internal readonly StorageClass Storage;
        internal readonly byte AuxiliaryCount;

        internal IMAGE_SYMBOL_ENTRY(MemoryMappedViewStream from)
        {
            Name = ASCIIEncoding.ASCII.GetString(Utils.AllocateBufferAndAssertRead(from, 8))
                .Replace('\0', ' ')
                .Trim();
            Value = Utils.ReadLittleEndianUInt32(from);
            SectionNumber = Utils.ReadLittleEndianShort(from);
            SymbolType = Utils.ReadLittleEndianUShort(from);
            Storage = (StorageClass)Utils.ReadByte(from);
            AuxiliaryCount = Utils.ReadByte(from);
        }

        internal void Dump(string prefix)
        {
            Utils.DebugTrace($"{prefix}Name : {Name}, value = {Value:X8}");
            Utils.DebugTrace($"{prefix}Storage {Storage} Type {SymbolType}");
        }

        internal enum StorageClass : byte
        {
            /// <summary>A special symbol that represents the end of function, for debugging purposes.</summary>
            EndOfFunction = byte.MaxValue,
            /// <summary>No assigned storage class.</summary>
            Null = 0,
            /// <summary>The automatic(stack) variable.The Value field specifies the stack frame offset.</summary>
            Automatic = 1,
            /// <summary>A value that Microsoft tools use for external symbols. The Value field indicates
            /// the size if the section number is IMAGE_SYM_UNDEFINED (0). If the section number is not
            /// zero, then the Value field specifies the offset within the section.</summary>
            External = 2,
            /// <summary>The offset of the symbol within the section.If the Value field is zero, then the
            /// symbol represents a section name.</summary>
            Static = 3,
            /// <summary>A register variable.The Value field specifies the register number.</summary>
            Register = 4,
            /// <summary>A symbol that is defined externally.</summary>
            ExternallyDefined = 5,
            /// <summary>A code label that is defined within the module. The Value field specifies the
            /// offset of the symbol within the section.</summary>
            Label = 6,
            /// <summary>A reference to a code label that is not defined.</summary>
            UndefinedLabel = 7,
            /// <summary>The structure member.The Value field specifies the n th member.</summary>
            StructureMember = 8,
            /// <summary>A formal argument (parameter) of a function. The Value field specifies the n th
            /// argument.</summary>
            FunctionFormalArgument = 9,
            /// <summary>The structure tag-name entry.</summary>
            StructureTag = 10,
            /// <summary>A union member.The Value field specifies the n th member.</summary>
            UnionMember = 11,
            /// <summary>The Union tag-name entry.</summary>
            UnionTag = 12,
            /// <summary>A Typedef entry.</summary>
            TypeDefinition = 13,
            /// <summary>A static data declaration.</summary>
            StaticData = 14,
            /// <summary>An enumerated type tagname entry.</summary>
            EnumeratedType = 15,
            /// <summary>A member of an enumeration.The Value field specifies the n th member.</summary>
            EnumeratedTypeMember = 16,
            /// <summary>A register parameter.</summary>
            RegisterParameter = 17,
            /// <summary>A bit-field reference. The Value field specifies the n th bit in the bit field.</summary>
            BitField = 18,
            /// <summary>A.bb (beginning of block) or.eb(end of block) record. The Value field is the
            /// relocatable address of the code location.</summary>
            ClassBlockBoundary = 100,
            /// <summary>A value that Microsoft tools use for symbol records that define the extent of a
            /// function: begin function(.bf ), end function( .ef ), and lines in function( .lf ).
            /// For.lf records, the Value field gives the number of source lines in the function. For.ef records,
            /// the Value field gives the size of the function code.</summary>
            ClassFunction = 101,
            /// <summary>An end-of-structure entry.</summary>
            EndOfStructure = 102,
            /// <summary>A value that Microsoft tools, as well as traditional COFF format, use for the
            /// source-file symbol record.The symbol is followed by auxiliary records that name the
            /// file.</summary>
            SourceFileSymbol = 103,
            /// <summary>A definition of a section (Microsoft tools use STATIC storage class instead).</summary>
            ClassSection = 104,
            /// <summary>A weak external.For more information, see Auxiliary Format 3: Weak Externals.</summary>
            WeakExternal = 105,
            /// <summary>A CLR token symbol. The name is an ASCII string that consists of the hexadecimal
            /// value of the token. For more information, see CLR Token Definition (Object Only).</summary>
            CLRToken = 107
        }
    }
}
