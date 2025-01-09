
using System.Runtime.InteropServices;

namespace PdbReader
{
    internal class GlobalSymbolsStream : HashStream
    {
        public GlobalSymbolsStream(Pdb owner, ushort index)
            : base(owner, index)
        {
        }

        internal override string StreamName => "GSI";
    }
}