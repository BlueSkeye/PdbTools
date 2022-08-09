using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    /// <summary>General format for pointer.</summary>
    /// <remarks>Structures are bytes aligned.</remarks>
    internal class Pointer : IPointer
    {
        internal PointerBody _body;
        internal byte[]? _symbolData;

        public PointerBody Body => _body;

        internal static Pointer Create(IndexedStream stream, PdbStreamReader reader,
            PointerBody body, uint remainingBytes)
        {
            Pointer result = new Pointer() {
                _body = body
            };
            //ushort symbolLength = reader.ReadUInt16();
            //uint endOffsetExcluded = symbolLength + reader.Offset;
            //LEAF_ENUM_e symbolKind;
            //result._object = stream.LoadRecord(uint.MinValue, symbolLength, out symbolKind);
            //if (reader.Offset != endOffsetExcluded) {
            //    throw new BugException("Invalid decoding of pointed to symbol.");
            //}
            if (0 < remainingBytes) {
                result._symbolData = new byte[remainingBytes];
                reader.ReadArray<byte>(result._symbolData, reader.ReadByte);
            }
            return result;
        }
    }
}
