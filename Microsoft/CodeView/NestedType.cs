using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class NestedType : INamedItem
    {
        private _NestedType _nestedType;

        public string Name { get; private set; }

        internal static NestedType Create(PdbStreamReader reader)
        {
            NestedType result = new NestedType() {
                _nestedType = reader.Read<_NestedType>(),
            };
            result.Name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _NestedType
        {
            internal LEAF_ENUM_e leaf; // LF_NESTTYPE
            // internal padding, must be 0
            internal byte Pad1;
            internal byte Pad2;
            internal uint /*CV_typ_t*/ index; // index of nested type definition
        }
    }
}
