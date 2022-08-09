using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class PointerToMember : IPointer
    {
        internal PointerBody _body;
        // index of containing class for pointer to member
        internal uint pmclass;
        // enumeration specifying pm format (CV_pmtype_e)
        internal CV_pmtype_e pmenum;

        public PointerBody Body => _body;

        internal static PointerToMember Create(PdbStreamReader reader, PointerBody body)
        {
            PointerToMember result = new PointerToMember() {
                _body = body,
                pmclass = reader.ReadUInt32(),
                pmenum = (CV_pmtype_e)reader.ReadUInt16()
            };
            return result;
        }
    }
}
