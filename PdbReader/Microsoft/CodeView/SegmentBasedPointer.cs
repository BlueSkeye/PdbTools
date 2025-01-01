using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class SegmentBasedPointer : TypeRecord, IPointer
    {
        internal PointerBody _body;
        // base segment if CV_PTR_BASE_SEG
        internal ushort bseg;

        public PointerBody Body => _body;

        public override LeafIndices LeafKind => LeafIndices.Pointer;

        internal static SegmentBasedPointer Create(PdbStreamReader reader, PointerBody rawBody,
            ref uint maxLength)
        {
            SegmentBasedPointer result = new SegmentBasedPointer() {
                _body = rawBody,
                bseg = reader.ReadUInt16()
            };
            Utils.SafeDecrement(ref maxLength, sizeof(ushort));
            return result;
        }
    }
}
