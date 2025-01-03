using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    /// <summary>General format for pointer.</summary>
    /// <remarks>Structures are bytes aligned.</remarks>
    internal class Pointer : TypeRecord, IPointer
    {
        internal PointerBody _body;
        internal byte[]? _symbolData;

        public PointerBody Body => _body;

        public override TypeKind LeafKind => TypeKind.Pointer;

        internal static Pointer Create(IndexedStream stream, PdbStreamReader reader, PointerBody body,
            ref uint maxLength)
        {
            Pointer result = new Pointer()
            {
                _body = body
            };
            //ushort symbolLength = reader.ReadUInt16();
            //uint endOffsetExcluded = symbolLength + reader.Offset;
            //LEAF_ENUM_e symbolKind;
            //result._object = stream.LoadRecord(uint.MinValue, symbolLength, out symbolKind);
            //if (reader.Offset != endOffsetExcluded) {
            //    throw new BugException("Invalid decoding of pointed to symbol.");
            //}
            if (0 < maxLength)
            {
                result._symbolData = new byte[maxLength];
                reader.ReadArray(result._symbolData, reader.ReadByte);
                maxLength = 0;
            }
            return result;
        }
    }
}
