
namespace PdbReader.Microsoft.CodeView
{
    internal class Structure : ClassOrStructureBase
    {
        private Structure(_Class @class, ulong structureSize, string name)
            : base(@class, structureSize, name)
        {
        }

        public override LeafIndices LeafKind => LeafIndices.Structure;

        private static ClassOrStructureBase Instanciate(_Class header, ulong structureSize, string itemName)
        {
            return new Structure(header, structureSize, itemName);
        }

        internal static Structure Create(PdbStreamReader reader, ref uint maxLength)
        {
            return (Structure)ClassOrStructureBase.Create(reader, ref maxLength, Instanciate);
        }
    }
}
