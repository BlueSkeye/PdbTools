
namespace LibProviderTester
{
    internal class BugException : ApplicationException
    {
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
