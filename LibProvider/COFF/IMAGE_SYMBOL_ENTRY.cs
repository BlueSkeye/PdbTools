using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace LibProvider.COFF
{
    internal class IMAGE_SYMBOL_ENTRY
    {
        internal readonly string Name; // 8 bytes
        internal readonly uint Value;
        internal readonly ushort SectionNumber;
        internal readonly ushort SymbolType;
        internal readonly byte StorageClass;
        internal readonly byte AuxiliaryCount;

        internal IMAGE_SYMBOL_ENTRY(MemoryMappedViewStream from)
        {
            Name = ASCIIEncoding.ASCII.GetString(Utils.AllocateBufferAndAssertRead(from, 8))
                .Replace('\0', ' ')
                .Trim();
            Value = Utils.ReadLittleEndianUInt32(from);
            SectionNumber = Utils.ReadBigEndianUShort(from);
            SymbolType = Utils.ReadBigEndianUShort(from);
            StorageClass = Utils.ReadByte(from);
            AuxiliaryCount = Utils.ReadByte(from);
        }
    }
}
