namespace PdbReader.Microsoft.CodeView.Types
{
    internal class Structure : ClassOrStructureBase
    {
        private Structure(_Class @class, ulong structureSize, string name)
            : base(@class, structureSize, name)
        {
        }

        public override TypeKind LeafKind => TypeKind.Structure;

        private static ClassOrStructureBase Instanciate(_Class header, ulong structureSize, string itemName)
        {
            return new Structure(header, structureSize, itemName);
        }

        internal static Structure Create(PdbStreamReader reader, ref uint maxLength)
        {
            return (Structure)Create(reader, ref maxLength, Instanciate);
        }
    }
}
