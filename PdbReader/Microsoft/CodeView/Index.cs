using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Index : TypeRecord, INamedItem
    {
        private const string ConstantName = "Index";
        internal _Index _data { get; private set; }

        public override LeafIndices LeafKind => LeafIndices.Index;

        public string Name => ConstantName;

        internal static Index Create(PdbStreamReader reader, ref uint maxLength)
        {
            Index result = new Index() {
                _data = reader.Read<_Index>()
            };
            Utils.SafeDecrement(ref maxLength, _Index.Size);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Index
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_Index>();
            internal LeafIndices leaf; // LF_INDEX
            internal ushort /*_2BYTEPAD*/ pad0; // internal padding, must be 0
            internal uint /*CV_ItemId*/ index; // type index of referenced leaf
        }
    }
}
