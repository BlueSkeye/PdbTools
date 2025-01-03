namespace PdbReader.Microsoft.CodeView.Types
{
    internal class Class : ClassOrStructureBase
    {
        private Class(_Class @class, ulong structureSize, string name)
            : base(@class, structureSize, name)
        {
        }

        public override TypeKind LeafKind => TypeKind.Class;

        private static ClassOrStructureBase Instanciate(_Class header, ulong structureSize, string itemName)
        {
            return new Class(header, structureSize, itemName);
        }

        internal static Class Create(PdbStreamReader reader, ref uint maxLength)
        {
            return (Class)Create(reader, ref maxLength, Instanciate);
        }
    }
}
