using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class TypeBasedPointer : IPointer
    {
        internal PointerBody _body;
        internal uint _index; // type index if CV_PTR_BASE_TYPE (CV_ptrtype_e.TypeBased)
        // Actually an array of characters (bytes).
        internal string _name; // name of base type

        public PointerBody Body => _body;

        internal static TypeBasedPointer Create(PdbStreamReader reader, PointerBody body,
            ref uint maxLength)
        {
            uint index = reader.ReadUInt32();
            Utils.SafeDecrement(ref maxLength, sizeof(uint));
            string name = reader.ReadNTBString(ref maxLength);
            TypeBasedPointer result = new TypeBasedPointer() {
                _body = body,
                _index = index,
                _name = name };
            return result;
        }
    }
}
