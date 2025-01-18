
namespace PdbReader.Microsoft.CodeView.Symbols
{
    internal class PROCREF : BaseSymbolRecord, IProcedureReference
    {
        internal uint _sumName;
        internal uint _symOffset;

        internal PROCREF(PdbStreamReader reader, ushort recordLength, SymbolKind kind)
            : base(reader.Owner, recordLength, kind)
        {
            _sumName = reader.ReadUInt32();
            _symOffset = reader.ReadUInt32();
            // Because module identifiers start at number 1, while internal indexing starts at 0.
            ModuleId = Utils.SafeCastToUint16(reader.ReadUInt16() - 1);
            Name = reader.ReadNTBString();
            return;
        }

        public ushort ModuleId { get; private set; }

        public string Name { get; private set; }

        private static readonly Type PROCSYM32Type = typeof(PROCSYM32);

        /// <summary>Returns the true procedure symbol this symbol is refering to.</summary>
        /// <returns></returns>
        /// <exception cref="BugException"></exception>
        public IProcedure GetProcedure()
        {
            ModuleSymbolStream symbolStream = base.Owner.EnsureModuleSymbolStreamIsLoadedInternal(ModuleId);
            ISymbolRecord symbol = symbolStream.GetSymbolByOffset(_symOffset);
            Type symbolType = symbol.GetType();
            if (object.ReferenceEquals(symbolType, PROCSYM32Type)) {
                PROCSYM32 result = (PROCSYM32)symbol;
                if (0 != string.Compare(result.Name, this.Name)) {
                    throw new ApplicationException(
                        $"Procedure reference name {this.Name} doesn't match resolved procedure name {result.Name}");
                }
                return result;
            }
            throw new BugException(
                $"Unexpected record type {symbolType.Name}. Was expected to have type {PROCSYM32Type.Name}");
        }
    }
}
