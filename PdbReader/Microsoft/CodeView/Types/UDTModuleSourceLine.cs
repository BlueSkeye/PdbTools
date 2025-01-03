using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class UDTModuleSourceLine : TypeRecord
    {
        private _UDTModuleSourceLine _udtModuleSourceLine;

        public override TypeKind LeafKind => TypeKind.UDTModuleSourceLine;

        // public string Name { get; private set; }

        internal static UDTModuleSourceLine Create(PdbStreamReader reader,
            ref uint maxLength)
        {
            UDTModuleSourceLine result = new UDTModuleSourceLine()
            {
                _udtModuleSourceLine = reader.Read<_UDTModuleSourceLine>(),
            };
            Utils.SafeDecrement(ref maxLength, _UDTModuleSourceLine.Size);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _UDTModuleSourceLine
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_UDTModuleSourceLine>();
            internal TypeKind leaf; // LF_UDT_MOD_SRC_LINE
            internal uint /*CV_typ_t*/ type; // UDT's type index
            internal uint /*CV_ItemId*/ src; // index into string table where source file name is saved
            internal uint line; // line number
            internal ushort imod; // module that contributes this UDT definition 
        }
    }
}
