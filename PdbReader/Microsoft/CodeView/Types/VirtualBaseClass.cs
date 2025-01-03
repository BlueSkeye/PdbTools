namespace PdbReader.Microsoft.CodeView.Types
{
    internal class VirtualBaseClass : VirtualBaseClassBase, INamedItem
    {
        private VirtualBaseClass(_VirtualBaseClass baseClass)
            : base(baseClass)
        {
        }

        public override TypeKind LeafKind => TypeKind.VBClass;

        internal static VirtualBaseClass Create(PdbStreamReader reader, ref uint maxLength)
        {
            VirtualBaseClassBase result = Create(reader, ref maxLength, Instanciate);
            return (VirtualBaseClass)result;
        }

        private static VirtualBaseClassBase Instanciate(_VirtualBaseClass baseClass)
        {
            return new VirtualBaseClass(baseClass);
        }
    }
}
