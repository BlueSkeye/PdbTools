using PdbReader.Microsoft.CodeView.Enumerations;
using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class MemberFunction : TypeRecord
    {
        private _MemberFunction _memberFunction;

        public override TypeKind LeafKind => TypeKind.MFunction;

        internal static MemberFunction Create(PdbStreamReader reader, ref uint maxLength)
        {
            MemberFunction result = new MemberFunction()
            {
                _memberFunction = reader.Read<_MemberFunction>(),
            };
            Utils.SafeDecrement(ref maxLength, _MemberFunction.Size);
            // result.Name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _MemberFunction
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_MemberFunction>();
            internal TypeKind leaf; // LF_MFUNCTION
            internal uint /*CV_typ_t*/ rvtype; // type index of return value
            internal uint /*CV_typ_t*/ classtype; // type index of containing class
            internal uint /*CV_typ_t*/ thistype; // type index of this pointer (model specific)
            internal CV_call_e calltype; // calling convention (call_t)
            internal CV_funcattr_t funcattr; // attributes
            internal ushort parmcount;      // number of parameters
            internal uint /*CV_typ_t*/ arglist; // type index of argument list
            internal int thisadjust; // this adjuster (long because pad required anyway)
        }
    }
}
