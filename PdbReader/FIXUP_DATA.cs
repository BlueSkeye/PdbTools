using System.Runtime.InteropServices;

namespace PdbReader
{
    // TODO : Undocumented structure.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FIXUP_DATA
    {
        internal _Flags Flags;
        internal uint Unknown1;
        internal uint Unknown2;

        [Flags()]
        public enum _Flags : uint
        {
            /// <summary>Seems that when this flag is set, Unknown2 may be a length, otherwise
            /// Unknown1 &lt Unknown2 and both are close to each other.</summary>
            HasLength = 0x80000000
        }
    }
}
