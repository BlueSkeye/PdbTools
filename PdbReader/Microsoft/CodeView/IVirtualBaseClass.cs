
namespace PdbReader.Microsoft.CodeView
{
    internal class IVirtualBaseClass : VirtualBaseClassBase, INamedItem
    {
        private IVirtualBaseClass(_VirtualBaseClass baseClass)
            : base(baseClass)
        {
        }

        public override LeafIndices LeafKind => LeafIndices.IVBClass;

        internal static IVirtualBaseClass Create(PdbStreamReader reader, ref uint maxLength)
        {
            VirtualBaseClassBase result = VirtualBaseClassBase.Create(reader, ref maxLength, Instanciate);
            return (IVirtualBaseClass)result;
        }

        private static VirtualBaseClassBase Instanciate(_VirtualBaseClass baseClass)
        {
            return new IVirtualBaseClass(baseClass);
        }
    }
}
