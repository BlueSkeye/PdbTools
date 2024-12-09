
namespace LibProvider
{
    internal static class Utils
    {
        internal static byte[] AllocateBufferAndAssertRead(Stream from, int length)
        {
            byte[] buffer = new byte[length];
            if (buffer.Length != from.Read(buffer, 0, buffer.Length)) {
                throw new BugException("Unexpected read length mismatch.");
            }
            return buffer;
        }

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

        internal static uint ParseOctalNumber(string candidate)
        {
            uint result = 0;
            string trimmed = candidate.Trim();
            if (0 == trimmed.Length) {
                throw new FormatException($"Invalid empty octal string.");
            }
            int trimmedLength = trimmed.Length;
            for (int index = 0; index < trimmedLength; index++) {
                char scannedCharacter = trimmed[index];
                result <<= 3;
                if ('0' > scannedCharacter || scannedCharacter > '7') {
                    throw new FormatException(
                        $"Invalid character '{scannedCharacter}' in octal string '{candidate}'.");
                }
                result += (uint)(scannedCharacter - '0');
            }
            return result;
        }

        internal static int SafeCastToInt32(long value)
        {
            if (int.MinValue > value) {
                throw new InvalidCastException($"{value} can't be casted to an int.");
            }
            if (int.MaxValue < value) {
                throw new InvalidCastException($"{value} can't be casted to an int.");
            }
            return (int)value;
        }

        internal static uint SafeCastToUInt32(int value)
        {
            if (0 > value) {
                throw new InvalidCastException($"{value} can't be casted to an int.");
            }
            return (uint)value;
        }

        internal static uint SafeCastToUInt32(long value)
        {
            if (0 > value) {
                throw new InvalidCastException($"{value} can't be casted to an uint.");
            }
            if (uint.MaxValue < value) {
                throw new InvalidCastException($"{value} can't be casted to an uint.");
            }
            return (uint)value;
        }
    }
}
