using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Member : TypeRecord, INamedItem
    {
        private const ushort MethodAccessMask = 0x0003;
        private const ushort MethodPropertiesMask = 0x0007;
        private const ushort MethodPropertiesShift = 0x0002;
        internal ulong _fieldOffset;
        internal _Member _member;
        // variable length offset of field followed
        // by length prefixed name of field
        internal string _name;

        public override LeafIndices LeafKind => LeafIndices.Member;

        internal CV_access_e MethodAccess
            => (CV_access_e)((ushort)_member.attr & MethodAccessMask);

        internal CV_methodprop_e MethodProperties
            => (CV_methodprop_e)(((ushort)_member.attr & MethodPropertiesMask) >> MethodPropertiesShift);
        
            public string Name => _name;

        internal static Member Create(PdbStreamReader reader, ref uint maxLength)
        {
            Member result = new Member();
            result._member = reader.Read<_Member>();
            Utils.SafeDecrement(ref maxLength, _Member.Size);
            /// Read field offset which is a variable length value.
            /// Algorithm is unclear and heuristically inferred.
            uint variantSize;
            result._fieldOffset = (ulong)reader.ReadVariant(out variantSize);
            Utils.SafeDecrement(ref maxLength, variantSize);
            result._name = reader.ReadNTBString(ref maxLength);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Member
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_Member>();
            internal LeafIndices _leaf; // LF_MEMBER
            internal CV_fldattr_t attr; // attribute mask
            internal uint /*CV_typ_t*/ index; // index of type record for field
        }
    }
}
