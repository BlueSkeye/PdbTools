using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class StaticMember : INamedItem
    {
        private _StaticMember _staticMember;
        // unsigned char Name[1];        // length prefixed name of field

        public string Name { get; private set; }

        internal static StaticMember Create(PdbStreamReader reader, ref uint maxLength)
        {
            StaticMember result = new StaticMember() {
                _staticMember = reader.Read<_StaticMember>(),
            };
            Utils.SafeDecrement(ref maxLength, _StaticMember.Size);
            result.Name = reader.ReadNTBString(ref maxLength);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _StaticMember
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_StaticMember>();
            internal LEAF_ENUM_e leaf; // LF_STMEMBER
            internal CV_fldattr_t attr; // attribute mask
            internal uint /*CV_typ_t*/ index; // index of type record for field
        }
    }
}
