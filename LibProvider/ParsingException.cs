
namespace LibProvider
{
    internal class ParsingException : ApplicationException
    {
        internal ParsingException(string message)
            : base(message)
        {
        }

        internal ParsingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
