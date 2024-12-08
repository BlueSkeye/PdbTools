
namespace PdbReader
{
    public class PDBFormatException : ApplicationException
    {
        internal PDBFormatException(string message)
            : base(message)
        {
        }

        internal PDBFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
