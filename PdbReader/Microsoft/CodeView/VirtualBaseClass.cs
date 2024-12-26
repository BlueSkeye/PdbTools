﻿
namespace PdbReader.Microsoft.CodeView
{
    internal class VirtualBaseClass : VirtualBaseClassBase, INamedItem, ILeafRecord
    {
        private VirtualBaseClass(_VirtualBaseClass baseClass)
            : base(baseClass)
        {
        }

        public LeafIndices LeafKind => LeafIndices.VBClass;

        internal static VirtualBaseClass Create(PdbStreamReader reader, ref uint maxLength)
        {
            VirtualBaseClassBase result = VirtualBaseClassBase.Create(reader, ref maxLength, Instanciate);
            return (VirtualBaseClass)result;
        }

        private static VirtualBaseClassBase Instanciate(_VirtualBaseClass baseClass)
        {
            return new VirtualBaseClass(baseClass);
        }
    }
}
