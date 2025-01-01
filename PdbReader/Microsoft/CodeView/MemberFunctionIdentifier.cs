using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class MemberFunctionIdentifier : TypeRecord
    {
        private _MemberFunctionIdentifier _memberFunctionIdentifier;
        // unsigned char name[CV_ZEROLEN];
        private string _name;

        internal static MemberFunctionIdentifier Create(PdbStreamReader reader,
            ref uint maxLength)
        {
            MemberFunctionIdentifier result = new MemberFunctionIdentifier() {
                _memberFunctionIdentifier = reader.Read<_MemberFunctionIdentifier>(),
            };
            Utils.SafeDecrement(ref maxLength, _MemberFunctionIdentifier.Size);
            result._name = reader.ReadNTBString(ref maxLength);
            return result;
        }

        public override LeafIndices LeafKind => LeafIndices.MFunctionIdentifier;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _MemberFunctionIdentifier
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_MemberFunctionIdentifier>();
            internal LeafIndices leaf; // LF_MFUNC_ID
            internal uint /*CV_typ_t*/ parentType; // type index of parent
            internal uint /*CV_typ_t*/ type; // function type
        }
    }
}
