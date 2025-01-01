using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class Modifier : TypeRecord
    {
        internal static readonly uint Size = (uint)Marshal.SizeOf<Modifier>();
        internal LeafIndices leaf; // LF_MODIFIER
        internal uint typeIndex; // modified type
        internal CV_modifier_t modifiers; // modifier attribute modifier_t
        internal ushort _unknown;

        public override LeafIndices LeafKind => LeafIndices.Modifier;

        internal static Modifier Create(PdbStreamReader reader, ref uint maxLength)
        {
            Modifier result = new Modifier() {
                leaf = (LeafIndices)reader.ReadUInt16(),
                typeIndex = reader.ReadUInt32(),
                modifiers = (CV_modifier_t)reader.ReadUInt16(),
                _unknown = reader.ReadUInt16()
            };
            return result;
        }
    }
}
