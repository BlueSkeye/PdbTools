
using System.Text;

namespace PdbReader
{
    internal static class Utils
    {
        private const int HexaFormatLineSize = 16;
        private const int HexaFormatSemilineSize = HexaFormatLineSize / 2;
        private static StringBuilder displayStringBuilder = new StringBuilder(HexaFormatLineSize);

        private static char GetDisplayCharacterForHexaDump(byte scannedByte)
        {
            return ((32 <= scannedByte) && (126 >= scannedByte)) ? (char)scannedByte : '.';
        }

        internal static StringBuilder HexadecimalFormat(uint blockOffset, byte[] data, int blockSize)
        {
            return HexadecimalFormat(new StringBuilder(), blockOffset, data, blockSize);
        }

        /// <summary></summary>
        /// <param name="into"></param>
        /// <param name="blockOffset"></param>
        /// <param name="data"></param>
        /// <param name="blockSize"></param>
        /// <returns></returns>
        /// <remarks>This method is not thread safe.</remarks>
        internal static StringBuilder HexadecimalFormat(StringBuilder into, uint blockOffset, byte[] data,
            int blockSize)
        {
            int relativeOffset = 0;
            while (relativeOffset < blockSize) {
                into.Append($"{(blockOffset + relativeOffset):X8} : ");
                int indexUpperBound = Math.Min(HexaFormatLineSize, (blockSize - relativeOffset));
                for(int index = 0; index < HexaFormatLineSize; /* index incremented inside the loop */) {
                    byte scannedByte = data[relativeOffset + index];
                    displayStringBuilder.Append(GetDisplayCharacterForHexaDump(scannedByte));
                    into.Append($"{scannedByte:X2} ");
                    if (0 == (++index % HexaFormatSemilineSize)) {
                        into.Append("- ");
                    }
                }
                into.AppendLine($"{displayStringBuilder.ToString()}");
                displayStringBuilder.Clear();
                relativeOffset += HexaFormatLineSize;
            }
            return into;
        }

        internal static int SafeCastToInt32(uint value)
        {
            if (int.MaxValue < value) { throw new BugException(); }
            return (int)value;
        }

        internal static ushort SafeCastToUint16(uint value)
        {
            if (ushort.MaxValue < value) { throw new BugException(); }
            return (ushort)value;
        }

        internal static uint SafeCastToUint32(long value)
        {
            if (0 > value) { throw new BugException(); }
            return SafeCastToUint32((ulong)value);
        }

        internal static uint SafeCastToUint32(ulong value)
        {
            if (uint.MaxValue < value) { throw new BugException(); }
            return (uint)value;
        }

        internal static void SafeDecrement(ref uint value, uint decrementBy)
        {
            if (value < decrementBy) {
                throw new BugException();
            }
            value -= decrementBy;
        }
    }
}
