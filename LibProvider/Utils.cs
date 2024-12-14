using System.IO.MemoryMappedFiles;
using System.Text;

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

        internal static void DebugTrace(string message)
        {
            Console.WriteLine(message);
        }

        internal static void Dump(string indentation, string title, ICollection<byte> data)
        {
            const int ItemsPerLine = 16;
            const int MiddleLineIndex = ItemsPerLine / 2;
            DebugTrace($"{indentation}{title}");
            StringBuilder builder = new StringBuilder();
            int index = 0;
            bool startLine = true;
            bool atMidline = false;
            while (index < data.Count) {
                if (startLine) {
                    if (0 != builder.Length) {
                        DebugTrace(builder.ToString());
                        builder.Clear();
                    }
                    builder.Append($"{indentation}{index:X8} ");
                    startLine = false;
                }
                if (atMidline) {
                    builder.Append("- ");
                    atMidline = false;
                }
                builder.Append($"{((byte)data.ElementAt(index)):X2} ");
                index++;
                if (0 == (index % ItemsPerLine)) {
                    startLine = true;
                }
                else if (0 == (index % MiddleLineIndex)) {
                    atMidline = true;
                }
            }
            if (0 != builder.Length) {
                DebugTrace(builder.ToString());
            }
        }

        internal static bool IsDebugFlagEnabled(ReaderProvider.DebugFlags wantedFlag,
            ReaderProvider.DebugFlags scannedFlag)
        {
            return (0 != (wantedFlag & scannedFlag));
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
        
        internal static int ReadAndParseInt32(MemoryMappedViewStream from, int inputLength)
        {
            string parsedString = ASCIIEncoding.ASCII.GetString(
                Utils.AllocateBufferAndAssertRead(from, inputLength)).Trim();
            return (string.Empty == parsedString) ? 0 : int.Parse(parsedString);
        }

        internal static uint ReadAndParseOctalUInt32(MemoryMappedViewStream from, int inputLength)
        {
            string parsedString = ASCIIEncoding.ASCII.GetString(
                Utils.AllocateBufferAndAssertRead(from, inputLength)).Trim();
            return (string.Empty == parsedString) ? 0 : Utils.ParseOctalNumber(parsedString);
        }

        internal static uint ReadAndParseUInt32(MemoryMappedViewStream from, int inputLength)
        {
            string parsedString = ASCIIEncoding.ASCII.GetString(
                Utils.AllocateBufferAndAssertRead(from, inputLength)).Trim();
            return (string.Empty == parsedString) ? 0 : uint.Parse(parsedString);
        }

        internal static uint ReadBigEndianUInt32(MemoryMappedViewStream from)
        {
            uint result = 0;
            for(int index = 0; sizeof(uint) > index; index++) {
                result <<= 8;
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a big endian uint.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (byte)inputByte;
            }
            return result;
        }

        internal static ushort ReadBigEndianUShort(MemoryMappedViewStream from)
        {
            ushort result = 0;
            for(int index = 0; sizeof(ushort) > index; index++) {
                result <<= 8;
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a big endian uint.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (byte)inputByte;
            }
            return result;
        }

        internal static byte ReadByte(MemoryMappedViewStream from)
        {
            int inputByte = from.ReadByte();
            if (0 > inputByte) {
                throw new ParsingException("EOF encountered while trying to read a byte.");
            }
            if (byte.MaxValue < inputByte) {
                throw new BugException("Unexpected value encounetered while reading a byte.");
            }
            return (byte)inputByte;
        }

        internal static short ReadLittleEndianShort(MemoryMappedViewStream from)
        {
            return (short)ReadLittleEndianUShort(from);
        }

        internal static ulong ReadLittleEndianUInt64(MemoryMappedViewStream from)
        {
            ulong result = 0;
            for(int index = 0; sizeof(ulong) > index; index++) {
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a little endian ulong.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (((ulong)(byte)inputByte) << (8 * index));
            }
            return result;
        }

        internal static uint ReadLittleEndianUInt32(MemoryMappedViewStream from)
        {
            uint result = 0;
            for(int index = 0; sizeof(uint) > index; index++) {
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a little endian uint.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (uint)((byte)inputByte << (8 * index));
            }
            return result;
        }

        internal static ushort ReadLittleEndianUShort(MemoryMappedViewStream from)
        {
            ushort result = 0;
            for(int index = 0; sizeof(ushort) > index; index++) {
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("EOF reached while reading a little endian uint.");
                }
                if (0 > inputByte) {
                    throw new BugException("Unexpected situation.");
                }
                result += (ushort)((byte)inputByte << (8 * index));
            }
            return result;
        }

        internal static string ReadNullTerminatedASCIIString(MemoryMappedViewStream from)
        {
            StringBuilder builder = new StringBuilder();
            while (true) {
                int inputByte = from.ReadByte();
                if (-1 == inputByte) {
                    throw new ParsingException("Unexpected EOF encountered while readind string.");
                }
                if (0 == inputByte) {
                    return builder.ToString();
                }
                builder.Append((char)inputByte);
            }
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

        internal static int SafeCastToInt32(uint value)
        {
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
