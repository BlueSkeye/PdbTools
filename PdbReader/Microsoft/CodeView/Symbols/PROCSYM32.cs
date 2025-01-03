
namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal class PROCSYM32 : BaseSymbolRecord
    {
        private readonly uint _parent; // pointer to the parent
        private readonly uint _end; // pointer to this blocks end
        private readonly uint _next; // pointer to next symbol
        private readonly uint _procedureLength; // Proc length
        private readonly uint _dbgStart; // Debug start offset
        private readonly uint _dbgEnd; // Debug end offset
        private readonly uint _typeIndexOrID; // Type index or ID
        private readonly uint _offset;
        private readonly ushort _segment;
        private readonly Flags _flags;

        internal PROCSYM32(PdbStreamReader reader, ushort recordLength, SymbolStream.SymbolKind symbolKind)
            : base(recordLength, symbolKind)
        {
            _parent = reader.ReadUInt32();
            _end = reader.ReadUInt32();
            _next = reader.ReadUInt32();
            _procedureLength = reader.ReadUInt32();
            _dbgStart = reader.ReadUInt32();
            _dbgEnd = reader.ReadUInt32();
            _typeIndexOrID = reader.ReadUInt32();
            _offset = reader.ReadUInt32();
            _segment = reader.ReadUInt16();
            _flags = (Flags)reader.ReadByte();
            Name = reader.ReadNTBString();
            return;
        }

        internal string Name { get; private set; }

        // CV_PROCFLAGS
        [Flags()]
        internal enum Flags : byte
        {
            CV_PFLAG_NOFPO = 0x01, // frame pointer present
            CV_PFLAG_INT = 0x02, // interrupt return
            CV_PFLAG_FAR = 0x04, // far return
            CV_PFLAG_NEVER = 0x08, // function does not return
            CV_PFLAG_NOTREACHED = 0x10, // label isn't fallen into
            CV_PFLAG_CUST_CALL = 0x20, // custom calling convention
            CV_PFLAG_NOINLINE = 0x40, // function marked as noinline
            CV_PFLAG_OPTDBGINFO = 0x80 // function has debug information for optimized code
        }
    }
}
