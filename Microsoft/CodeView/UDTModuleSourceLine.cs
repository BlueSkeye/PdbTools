using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class UDTModuleSourceLine
    {
        private _UDTModuleSourceLine _udtModuleSourceLine;

        // public string Name { get; private set; }

        internal static UDTModuleSourceLine Create(PdbStreamReader reader)
        {
            UDTModuleSourceLine result = new UDTModuleSourceLine() {
                _udtModuleSourceLine = reader.Read<_UDTModuleSourceLine>(),
            };
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _UDTModuleSourceLine
        {
            internal LEAF_ENUM_e leaf; // LF_UDT_MOD_SRC_LINE
            internal uint /*CV_typ_t*/ type; // UDT's type index
            internal uint /*CV_ItemId*/ src; // index into string table where source file name is saved
            internal uint line; // line number
            internal ushort imod; // module that contributes this UDT definition 
        }
    }
}
