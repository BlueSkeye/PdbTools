using PdbReader.Microsoft.CodeView.Enumerations;
using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class Enumeration : TypeRecord
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

        public override TypeKind LeafKind => TypeKind.Enum;

        internal static Enumeration Create(PdbStreamReader reader, ref uint maxLength)
        {
            _Enumeration core = reader.Read<_Enumeration>();
            Utils.SafeDecrement(ref maxLength, _Enumeration.Size);
            string name = reader.ReadNTBString(ref maxLength);
            string decoratedName = reader.ReadNTBString(ref maxLength);
            return new Enumeration(core, name, decoratedName);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Enumeration
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_Enumeration>();
            internal TypeKind leaf; // LF_ENUM
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field
            internal uint /*CV_typ_t*/ utype; // underlying type of the enum
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
        }
    }
}
