using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class TypeBasedPointer : IPointer
    {
        internal PointerBody _body;
        internal uint index; // type index if CV_PTR_BASE_TYPE (CV_ptrtype_e.TypeBased)
        // Actually an array of characters (bytes).
        internal string _name; // name of base type

        public PointerBody Body => _body;

        internal static TypeBasedPointer Create(PdbStreamReader reader, PointerBody body)
        {
            TypeBasedPointer result = new TypeBasedPointer() {
                _body = body,
                index = reader.ReadUInt32(),
                _name = reader.ReadNTBString()
            };
            return result;
        }
    }
}
