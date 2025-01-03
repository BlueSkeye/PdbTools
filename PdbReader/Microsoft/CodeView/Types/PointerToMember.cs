using PdbReader.Microsoft.CodeView.Enumerations;
using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class PointerToMember : TypeRecord, IPointer
    {
        internal PointerBody _body;
        // index of containing class for pointer to member
        internal uint _pmclass;
        // enumeration specifying pm format (CV_pmtype_e)
        internal CV_pmtype_e _pmenum;

        public PointerBody Body => _body;

        public override TypeKind LeafKind => TypeKind.Pointer;

        internal static PointerToMember Create(PdbStreamReader reader, PointerBody body,
            ref uint maxLength)
        {
            uint pmClass = reader.ReadUInt32();
            Utils.SafeDecrement(ref maxLength, sizeof(uint));
            CV_pmtype_e pmenum = (CV_pmtype_e)reader.ReadUInt16();
            Utils.SafeDecrement(ref maxLength, sizeof(ushort));
            PointerToMember result = new PointerToMember()
            {
                _body = body,
                _pmclass = pmClass,
                _pmenum = pmenum
            };
            return result;
        }
    }
}
