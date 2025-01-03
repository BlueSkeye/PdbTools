using PdbReader.Microsoft.CodeView.Enumerations;
using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class Modifier : TypeRecord
    {
        internal static readonly uint Size = (uint)Marshal.SizeOf<Modifier>();
        internal TypeKind leaf; // LF_MODIFIER
        internal uint typeIndex; // modified type
        internal CV_modifier_t modifiers; // modifier attribute modifier_t
        internal ushort _unknown;

        public override TypeKind LeafKind => TypeKind.Modifier;

        internal static Modifier Create(PdbStreamReader reader, ref uint maxLength)
        {
            Modifier result = new Modifier()
            {
                leaf = (TypeKind)reader.ReadUInt16(),
                typeIndex = reader.ReadUInt32(),
                modifiers = (CV_modifier_t)reader.ReadUInt16(),
                _unknown = reader.ReadUInt16()
            };
            return result;
        }
    }
}
