
namespace LibProviderTester
{
    internal static class Utils
    {
        internal static T AssertArgumentNotNull<T>(T? candidate, string argumentName)
        {
            if (null == candidate) {
                throw new ArgumentNullException(
                    $"Unexpected null {typeof(T).FullName} reference encountered for argument {argumentName}.");
            }
            return candidate;
        }

        internal static T AssertNotNull<T>(T? candidate)
        {
            if (null == candidate) {
                throw new BugException($"Unexpected null {typeof(T).FullName} reference encountered.");
            }
            return candidate;
        }
    }
}
