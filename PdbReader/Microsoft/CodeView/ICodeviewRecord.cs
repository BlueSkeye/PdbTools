
namespace PdbReader.Microsoft.CodeView
{
    internal interface ICodeviewRecord
    {
        RecordType Type { get; }

        internal enum RecordType
        {
            UNDEFINED = 0,
            Type,
            Symbol
        }
    }
}
