
using PdbReader.Microsoft.CodeView;

namespace PdbReader
{
    /// <summary>Also known as the TPI stream. Contains Codeview type records.</summary>
    internal class TPIStream : TypeIndexedStream
    {
        private const ushort ThisStreamIndex = 2;

        internal TPIStream(Pdb owner)
            : base(owner, ThisStreamIndex)
        {
            base.LoadTypeRecords();
        }

        internal override string StreamName => "TPI";
    }
}
