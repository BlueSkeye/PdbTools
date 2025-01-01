
using PdbReader.Microsoft.CodeView;

namespace PdbReader
{
    /// <summary>Also known as the TPI stream. Contains Codeview type records.</summary>
    public class TPIStream : IndexedStream
    {
        private const ushort ThisStreamIndex = 2;

        public TPIStream(Pdb owner)
            : base(owner, ThisStreamIndex)
        {
            base.LoadRecords(ICodeviewRecord.RecordType.Type);
        }

        internal override string StreamName => "TPI";
    }
}
