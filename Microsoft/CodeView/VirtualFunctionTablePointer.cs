using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class VirtualFunctionTablePointer : INamedItem
    {
        private _VirtualFunctionTablePointer _virtualFunctionTablePointer;

        public string Name => INamedItem.NoName;

        internal static VirtualFunctionTablePointer Create(PdbStreamReader reader)
        {
            VirtualFunctionTablePointer result = new VirtualFunctionTablePointer() {
                _virtualFunctionTablePointer = reader.Read<_VirtualFunctionTablePointer>(),
            };
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _VirtualFunctionTablePointer
        {
            internal LEAF_ENUM_e leaf; // LF_VFUNCTAB
            internal ushort Pad0; // internal padding, must be 0
            internal uint /*CV_typ_t*/ type; // type index of pointer
        }
    }
}
