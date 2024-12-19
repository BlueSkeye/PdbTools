
using System.Text;

namespace PdbReader
{
    internal static class Utils
    {
        internal static StringBuilder HexadecimalFormat(uint blockOffset, byte[] data, int blockSize)
        {
            return HexadecimalFormat(new StringBuilder(), blockOffset, data, blockSize);
        }

        internal static StringBuilder HexadecimalFormat(StringBuilder into, uint blockOffset, byte[] data,
            int blockSize)
        {
            const int LineSize = 16;
            const int SemilineSize = LineSize / 2;
            int relativeOffset = 0;
            while (relativeOffset < blockSize) {
                into.Append($"{(blockOffset + relativeOffset):X8} : ");
                for(int index = 0; index < LineSize; index++) {
                    into.Append($"{data[relativeOffset]:X2} ");
                    if (0 == (index % SemilineSize)) {
                        into.Append("- ");
                    }
                }
                into.AppendLine();
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
