﻿namespace PdbReader
{
    // Sorted by values for ease of missing kind diagnostic.
    /// <summary></summary>
    /// <remarks>Taken from SYM_ENUM_e
    /// in https://github.com/microsoft/microsoft-pdb/blob/master/include/cvinfo.h</remarks>
    public enum SymbolKind : ushort
    {
        S_COMPILE = 0x0001,  // Compile flags symbol
        S_REGISTER_16t = 0x0002,  // Register variable
        S_CONSTANT_16t = 0x0003,  // constant symbol
        S_UDT_16t = 0x0004,  // User defined type
        S_SSEARCH = 0x0005,  // Start Search
        S_END = 0x0006,
        S_SKIP = 0x0007,  // Reserve symbol space in $$Symbols table
        S_CVRESERVE = 0x0008,  // Reserved symbol for CV internal use
        S_OBJNAME_ST = 0x0009,  // path to object file name
        S_ENDARG = 0x000A,  // end of argument/return list
        S_COBOLUDT_16t = 0x000B,  // special UDT for cobol that does not symbol pack
        S_MANYREG_16t = 0x000C,  // multiple register variable
        S_RETURN = 0x000D,  // return description symbol
        S_ENTRYTHIS = 0x000E,  // description of this pointer on entry

        S_BPREL16 = 0x0100,  // BP-relative
        S_LDATA16 = 0x0101,  // Module-local symbol
        S_GDATA16 = 0x0102,  // Global data symbol
        S_PUB16 = 0x0103,  // a public symbol
        S_LPROC16 = 0x0104,  // Local procedure start
        S_GPROC16 = 0x0105,  // Global procedure start
        S_THUNK16 = 0x0106,  // Thunk Start
        S_BLOCK16 = 0x0107,  // block start
        S_WITH16 = 0x0108,  // with start
        S_LABEL16 = 0x0109,  // code label
        S_CEXMODEL16 = 0x010A,  // change execution model
        S_VFTABLE16 = 0x010B,  // address of virtual function table
        S_REGREL16 = 0x010C,  // register relative address

        S_BPREL32_16t = 0x0200,  // BP-relative
        S_LDATA32_16t = 0x0201,  // Module-local symbol
        S_GDATA32_16t = 0x0202,  // Global data symbol
        S_PUB32_16t = 0x0203,  // a public symbol (CV internal reserved)
        S_LPROC32_16t = 0x0204,  // Local procedure start
        S_GPROC32_16t = 0x0205,  // Global procedure start
        S_THUNK32_ST = 0x0206,  // Thunk Start
        S_BLOCK32_ST = 0x0207,  // block start
        S_WITH32_ST = 0x0208,  // with start
        S_LABEL32_ST = 0x0209,  // code label
        S_CEXMODEL32 = 0x020A,  // change execution model
        S_VFTABLE32_16t = 0x020B,  // address of virtual function table
        S_REGREL32_16t = 0x020C,  // register relative address
        S_LTHREAD32_16t = 0x020D,  // local thread storage
        S_GTHREAD32_16t = 0x020E,  // global thread storage
        S_SLINK32 = 0x020F,  // static link for MIPS EH implementation

        S_LPROCMIPS_16t = 0x0300,  // Local procedure start
        S_GPROCMIPS_16t = 0x0301,  // Global procedure start

        // if these ref symbols have names following then the names are in ST format
        S_PROCREF_ST = 0x0400,  // Reference to a procedure
        S_DATAREF_ST = 0x0401,  // Reference to data
        S_ALIGN = 0x0402,  // Used for page alignment of symbols

        S_LPROCREF_ST = 0x0403,  // Local Reference to a procedure
        S_OEM = 0x0404,  // OEM defined symbol            

        // sym records with 32-bit types embedded instead of 16-bit
        // all have 0x1000 bit set for easy identification
        // only do the 32-bit target versions since we don't really
        // care about 16-bit ones anymore.
        S_TI16_MAX = 0x1000,

        S_REGISTER_ST = 0x1001,  // Register variable
        S_CONSTANT_ST = 0x1002,  // constant symbol
        S_UDT_ST = 0x1003,  // User defined type
        S_COBOLUDT_ST = 0x1004,  // special UDT for cobol that does not symbol pack
        S_MANYREG_ST = 0x1005,  // multiple register variable
        S_BPREL32_ST = 0x1006,  // BP-relative
        S_LDATA32_ST = 0x1007,  // Module-local symbol
        S_GDATA32_ST = 0x1008,  // Global data symbol
        S_PUB32_ST = 0x1009,  // a public symbol (CV internal reserved)
        S_LPROC32_ST = 0x100A,  // Local procedure start
        S_GPROC32_ST = 0x100B,  // Global procedure start
        S_VFTABLE32 = 0x100C,  // address of virtual function table
        S_REGREL32_ST = 0x100D,  // register relative address
        S_LTHREAD32_ST = 0x100E,  // local thread storage
        S_GTHREAD32_ST = 0x100F,  // global thread storage

        S_LPROCMIPS_ST = 0x1010,  // Local procedure start
        S_GPROCMIPS_ST = 0x1011,  // Global procedure start
        S_FRAMEPROC = 0x1012,  // extra frame and proc information
        S_COMPILE2_ST = 0x1013,  // extended compile flags and info
                                    // new symbols necessary for 16-bit enumerates of IA64 registers
                                    // and IA64 specific symbols

        S_MANYREG2_ST = 0x1014,  // multiple register variable
        S_LPROCIA64_ST = 0x1015,  // Local procedure start (IA64)
        S_GPROCIA64_ST = 0x1016,  // Global procedure start (IA64)

        // Local symbols for IL
        S_LOCALSLOT_ST = 0x1017,  // local IL sym with field for local slot index
        S_PARAMSLOT_ST = 0x1018,  // local IL sym with field for parameter slot index

        S_ANNOTATION = 0x1019,  // Annotation string literals

        // symbols to support managed code debugging
        S_GMANPROC_ST = 0x101A,  // Global proc
        S_LMANPROC_ST = 0x101B,  // Local proc
        S_RESERVED1 = 0x101C,  // reserved
        S_RESERVED2 = 0x101D,  // reserved
        S_RESERVED3 = 0x101E,  // reserved
        S_RESERVED4 = 0x101F,  // reserved
        S_LMANDATA_ST = 0x1020,
        S_GMANDATA_ST = 0x1021,
        S_MANFRAMEREL_ST = 0x1022,
        S_MANREGISTER_ST = 0x1023,
        S_MANSLOT_ST = 0x1024,
        S_MANMANYREG_ST = 0x1025,
        S_MANREGREL_ST = 0x1026,
        S_MANMANYREG2_ST = 0x1027,
        S_MANTYPREF = 0x1028,  // Index for type referenced by name from metadata
        S_UNAMESPACE_ST = 0x1029,  // Using namespace

        // Symbols w/ SZ name fields. All name fields contain utf8 encoded strings.
        S_ST_MAX = 0x1100,  // starting point for SZ name symbols
        S_OBJNAME = 0x1101,  // path to object file name
        S_THUNK32 = 0x1102,  // Thunk Start
        S_BLOCK32 = 0x1103,  // block start
        S_WITH32 = 0x1104,  // with start
        S_LABEL32 = 0x1105,  // code label
        S_REGISTER = 0x1106,  // Register variable
        S_CONSTANT = 0x1107,  // constant symbol
        S_UDT = 0x1108,  // User defined type
        S_COBOLUDT = 0x1109,  // special UDT for cobol that does not symbol pack
        S_MANYREG = 0x110A,  // multiple register variable
        S_BPREL32 = 0x110B,  // BP-relative
        S_LDATA32 = 0x110C,  // Module-local symbol
        S_GDATA32 = 0x110D,  // Global data symbol
        S_PUB32 = 0x110E,  // a public symbol (CV internal reserved)
        S_LPROC32 = 0x110F,  // Local procedure start
        S_GPROC32 = 0x1110,  // Global procedure start
        S_REGREL32 = 0x1111,  // register relative address
        S_LTHREAD32 = 0x1112,  // local thread storage
        S_GTHREAD32 = 0x1113,  // global thread storage

        S_LPROCMIPS = 0x1114,  // Local procedure start
        S_GPROCMIPS = 0x1115,  // Global procedure start
        S_COMPILE2 = 0x1116,  // extended compile flags and info
        S_MANYREG2 = 0x1117,  // multiple register variable
        S_LPROCIA64 = 0x1118,  // Local procedure start (IA64)
        S_GPROCIA64 = 0x1119,  // Global procedure start (IA64)
        S_LOCALSLOT = 0x111A,  // local IL sym with field for local slot index
        S_SLOT = S_LOCALSLOT,  // alias for LOCALSLOT
        S_PARAMSLOT = 0x111B,  // local IL sym with field for parameter slot index

        // symbols to support managed code debugging
        S_LMANDATA = 0x111C,
        S_GMANDATA = 0x111D,
        S_MANFRAMEREL = 0x111E,
        S_MANREGISTER = 0x111F,
        S_MANSLOT = 0x1120,
        S_MANMANYREG = 0x1121,
        S_MANREGREL = 0x1122,
        S_MANMANYREG2 = 0x1123,
        S_UNAMESPACE = 0x1124,  // Using namespace

        // ref symbols with name fields
        S_PROCREF = 0x1125,  // Reference to a procedure
        S_DATAREF = 0x1126,  // Reference to data
        S_LPROCREF = 0x1127,  // Local Reference to a procedure
        S_ANNOTATIONREF = 0x1128,  // Reference to an S_ANNOTATION symbol
        S_TOKENREF = 0x1129,  // Reference to one of the many MANPROCSYM's

        // continuation of managed symbols
        S_GMANPROC = 0x112A,  // Global proc
        S_LMANPROC = 0x112B,  // Local proc

        // short, light-weight thunks
        S_TRAMPOLINE = 0x112C,  // trampoline thunks
        S_MANCONSTANT = 0x112D,  // constants with metadata type info

        // native attributed local/parms
        S_ATTR_FRAMEREL = 0x112E,  // relative to virtual frame ptr
        S_ATTR_REGISTER = 0x112F,  // stored in a register
        S_ATTR_REGREL = 0x1130,  // relative to register (alternate frame ptr)
        S_ATTR_MANYREG = 0x1131,  // stored in >1 register

        // Separated code (from the compiler) support
        S_SEPCODE = 0x1132,

        S_LOCAL_2005 = 0x1133,  // defines a local symbol in optimized code
        S_DEFRANGE_2005 = 0x1134,  // defines a single range of addresses in which symbol can be evaluated
        S_DEFRANGE2_2005 = 0x1135,  // defines ranges of addresses in which symbol can be evaluated

        S_SECTION = 0x1136,  // A COFF section in a PE executable
        S_COFFGROUP = 0x1137,  // A COFF group
        S_EXPORT = 0x1138,  // A export

        S_CALLSITEINFO = 0x1139,  // Indirect call site information
        S_FRAMECOOKIE = 0x113A,  // Security cookie information

        S_DISCARDED = 0x113B,  // Discarded by LINK /OPT:REF (experimental, see richards)

        S_COMPILE3 = 0x113C,  // Replacement for S_COMPILE2
        S_ENVBLOCK = 0x113D,  // Environment block split off from S_COMPILE2

        S_LOCAL = 0x113E,  // defines a local symbol in optimized code
        S_DEFRANGE = 0x113F,  // defines a single range of addresses in which symbol can be evaluated
        S_DEFRANGE_SUBFIELD = 0x1140,           // ranges for a subfield

        S_DEFRANGE_REGISTER = 0x1141,           // ranges for en-registered symbol
        S_DEFRANGE_FRAMEPOINTER_REL = 0x1142,   // range for stack symbol.
        S_DEFRANGE_SUBFIELD_REGISTER = 0x1143,  // ranges for en-registered field of symbol
        S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE = 0x1144, // range for stack symbol span valid full scope of function body, gap might apply.
        S_DEFRANGE_REGISTER_REL = 0x1145, // range for symbol address as register + offset.

        // S_PROC symbols that reference ID instead of type
        S_LPROC32_ID = 0x1146,
        S_GPROC32_ID = 0x1147,
        S_LPROCMIPS_ID = 0x1148,
        S_GPROCMIPS_ID = 0x1149,
        S_LPROCIA64_ID = 0x114A,
        S_GPROCIA64_ID = 0x114B,

        S_BUILDINFO = 0x114C, // build information.
        S_INLINESITE = 0x114D, // inlined function callsite.
        S_INLINESITE_END = 0x114E,
        S_PROC_ID_END = 0x114f,

        S_DEFRANGE_HLSL = 0x1150,
        S_GDATA_HLSL = 0x1151,
        S_LDATA_HLSL = 0x1152,

        S_FILESTATIC = 0x1153,

        S_LOCAL_DPC_GROUPSHARED = 0x1154, // DPC groupshared variable
        S_LPROC32_DPC = 0x1155, // DPC local procedure start
        S_LPROC32_DPC_ID =  0x1156,
        S_DEFRANGE_DPC_PTR_TAG =  0x1157, // DPC pointer tag definition range
        S_DPC_SYM_TAG_MAP = 0x1158, // DPC pointer tag value to symbol record map

        S_ARMSWITCHTABLE = 0x1159,
        S_CALLEES = 0x115A,
        S_CALLERS = 0x115B,
        S_POGODATA = 0x115C,
        S_INLINESITE2 = 0x115D,      // extended inline site information
        S_HEAPALLOCSITE = 0x115E,    // heap allocation site
        S_MOD_TYPEREF = 0x115F, // only generated at link time

        S_REF_MINIPDB = 0x1160, // only generated at link time for mini PDB
        S_PDBMAP = 0x1161, // only generated at link time for mini PDB

        S_GDATA_HLSL32 = 0x1162,
        S_LDATA_HLSL32 = 0x1163,

        S_GDATA_HLSL32_EX = 0x1164,
        S_LDATA_HLSL32_EX = 0x1165,

        S_RECTYPE_MAX,               // one greater than last
        S_RECTYPE_LAST = S_RECTYPE_MAX - 1,
        S_RECTYPE_PAD = S_RECTYPE_MAX + 0x100, // Used *only* to verify symbol record types so that current PDB code can potentially read
                            // future PDBs (assuming no format change, etc).

        S_FASTLINK = 0x1167,
        S_INLINEES = 0x1168,
    }
}
