using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Method : INamedItem
    {
        private _Method _method;
        // unsigned char Name[1];        // length prefixed name of method

        public string Name { get; private set; }

        internal static Method Create(PdbStreamReader reader)
        {
            Method result = new Method() {
                _method = reader.Read<_Method>(),
            };
            result.Name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Method
        {
            internal LEAF_ENUM_e leaf; // LF_METHOD
            internal ushort count; // number of occurrences of function
            internal uint /*CV_typ_t*/ mList; // index to LF_METHODLIST record
        }
    }
}
