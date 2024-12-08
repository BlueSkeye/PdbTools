
namespace PdbReader
{
    internal class BugException : ApplicationException
    {
        internal BugException()
        {
        }

        internal BugException(string message)
            : base(message)
        {
        }

        internal BugException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
