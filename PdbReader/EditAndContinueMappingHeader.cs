using System.Runtime.InteropServices;

namespace PdbReader
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct EditAndContinueMappingHeader
    {
        internal const uint SignatureValue = 0xEFFEEFFE;

        // Should be 0xEFFEEFFE
        internal uint Signature;
        internal uint Unkown1;
        internal uint StringPoolBytesSize;
        internal byte Unknown3;
    }
}
