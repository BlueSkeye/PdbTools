using System.Runtime.InteropServices;

namespace PdbReader.TypeRecords
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TypeRecordHeader
    {
        /// <summary>Record length, NOT including this 2 byte field.</summary>
        internal ushort RecordLength;
        /// <summary>Record kind enumeration.</summary>
        internal _Kind RecordKind;

        /// <summary>Sorted by identifier.</summary>
        internal enum _Kind : ushort
        {
            VTShape = 0x000A,
            Modifier = 0x1001,
            Pointer = 0x1002,
            Procedure = 0x1008,
            MFunction = 0x1009,
            ArgumentList = 0x1201,
            FieldList = 0x1203,
            BitField = 0x1205,
            MethodList = 0x1206,
            BClass = 0x1400,
            VBClass = 0x1401,
            IVBClass = 0x1402,
            VFuncTab = 0x1409,
            Enumerate = 0x1502,
            Array = 0x1503,
            Class = 0x1504,
            Structure = 0x1505,
            Enum = 0x1507,
            Member = 0x150D,
            Method = 0x150F,
            NestType = 0x1510,
            StMember = 0x150E,
            OneMethod = 0x1511,
            NestTypeEx = 0x1512,
            Interface = 0x1519,
            FuncId = 0x1601,
            BuildInfo = 0x1603,
            SubstrList = 0x1604,
            StringId = 0x1605,
            Union = 0x1506,
        }
    }
}
