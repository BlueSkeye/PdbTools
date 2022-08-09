using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class SegmentBasedPointer : IPointer
    {
        internal PointerBody _body;
        // base segment if CV_PTR_BASE_SEG
        internal ushort bseg;

        public PointerBody Body => _body;

        internal static SegmentBasedPointer Create(PdbStreamReader reader, PointerBody rawBody)
        {
            SegmentBasedPointer result = new SegmentBasedPointer() {
                _body = rawBody,
                bseg = reader.ReadUInt16()
            };
            return result;
        }
    }
}
