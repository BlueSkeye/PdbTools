namespace PdbReader.Microsoft.CodeView.Types
{
    internal class IVirtualBaseClass : VirtualBaseClassBase, INamedItem
    {
        private IVirtualBaseClass(_VirtualBaseClass baseClass)
            : base(baseClass)
        {
        }

        public override TypeKind LeafKind => TypeKind.IVBClass;

        internal static IVirtualBaseClass Create(PdbStreamReader reader, ref uint maxLength)
        {
            VirtualBaseClassBase result = Create(reader, ref maxLength, Instanciate);
            return (IVirtualBaseClass)result;
        }

        private static VirtualBaseClassBase Instanciate(_VirtualBaseClass baseClass)
        {
            return new IVirtualBaseClass(baseClass);
        }
    }
}
