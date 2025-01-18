
namespace PdbReader.Microsoft.CodeView
{
    public interface ICodeviewRecord
    {
        RecordType Type { get; }

        public enum RecordType
        {
            UNDEFINED = 0,
            Type,
            Symbol
        }
    }
}
