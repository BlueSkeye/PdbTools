﻿
namespace PdbReader.Microsoft.CodeView
{
    internal class Class : ClassOrStructureBase, ILeafRecord
    {
        private Class(_Class @class, ulong structureSize, string name)
            : base(@class, structureSize, name)
        {
        }

        public LeafIndices LeafKind => LeafIndices.Class;

        private static ClassOrStructureBase Instanciate(_Class header, ulong structureSize, string itemName)
        {
            return new Class(header, structureSize, itemName);
        }

        internal static Class Create(PdbStreamReader reader, ref uint maxLength)
        {
            return (Class)ClassOrStructureBase.Create(reader, ref maxLength, Instanciate);
        }
    }
}
