using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader.Microsoft.CodeView
{
    internal class Enumeration
    {
        internal _Enumeration _data;
        internal string _name;
        internal string _decoratedName;

        private Enumeration(_Enumeration data, string name, string decoratedName)
        {
            _data = data;
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _decoratedName = decoratedName
                ?? throw new ArgumentNullException(nameof(decoratedName));
        }

        internal static Enumeration Create(PdbStreamReader reader)
        {
            return new Enumeration(reader.Read<_Enumeration>(), reader.ReadNTBString(),
                reader.ReadNTBString());
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Enumeration
        {
            internal LEAF_ENUM_e leaf; // LF_ENUM
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field
            internal uint /*CV_typ_t*/ utype; // underlying type of the enum
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
        }
    }
}
