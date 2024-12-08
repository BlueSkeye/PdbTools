using System.Runtime.InteropServices;

namespace PdbReader
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct StringPoolHeader
    {
        internal static readonly uint Size = (uint)Marshal.SizeOf<StringPoolHeader>();
        internal const uint StringPoolHeaderSignature = 0xEFFEEFFE;

        internal uint Signature;
        internal uint HashVersion; // 1 or 2
        // Number of bytes of names buffer.
        internal uint ByteSize;
    }
}
