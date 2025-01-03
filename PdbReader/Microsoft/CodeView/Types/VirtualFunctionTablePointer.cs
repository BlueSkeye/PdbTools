using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class VirtualFunctionTablePointer : TypeRecord, INamedItem
    {
        private _VirtualFunctionTablePointer _virtualFunctionTablePointer;

        public override TypeKind LeafKind => TypeKind.VFunctionTAB;

        public string Name => INamedItem.NoName;

        internal static VirtualFunctionTablePointer Create(PdbStreamReader reader,
            ref uint maxLength)
        {
            VirtualFunctionTablePointer result = new VirtualFunctionTablePointer()
            {
                _virtualFunctionTablePointer = reader.Read<_VirtualFunctionTablePointer>(),
            };
            Utils.SafeDecrement(ref maxLength, _VirtualFunctionTablePointer.Size);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _VirtualFunctionTablePointer
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_VirtualFunctionTablePointer>();
            internal TypeKind leaf; // LF_VFUNCTAB
            internal ushort Pad0; // internal padding, must be 0
            internal uint /*CV_typ_t*/ type; // type index of pointer
        }
    }
}
