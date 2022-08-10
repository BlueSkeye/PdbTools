using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class MemberFunctionIdentifier
    {
        private _MemberFunctionIdentifier _memberFunctionIdentifier;
        // unsigned char name[CV_ZEROLEN];
        private string _name;

        internal static MemberFunctionIdentifier Create(PdbStreamReader reader)
        {
            MemberFunctionIdentifier result = new MemberFunctionIdentifier() {
                _memberFunctionIdentifier = reader.Read<_MemberFunctionIdentifier>(),
            };
            result._name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _MemberFunctionIdentifier
        {
            internal LEAF_ENUM_e leaf; // LF_MFUNC_ID
            internal uint /*CV_typ_t*/ parentType; // type index of parent
            internal uint /*CV_typ_t*/ type; // function type
        }
    }
}
