using System.Runtime.InteropServices;

namespace PdbReader
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FPO_DATA
    {
        // offset 1st byte of function code
        internal uint ulOffStart;
        // # bytes in function
        internal uint cbProcSize;
        // # bytes in locals/4
        internal uint cdwLocals;
        // # bytes in params/4
        internal ushort cdwParams;
        // # bytes in prolog
        internal byte cbProlog;
        internal _Flags Flags;

        [Flags()]
        public enum _Flags : byte
        {
            NoRegSaved = 0x00,
            OneRegSaved = 0x01,
            TwoRegsSaved = 0x02,
            ThreeRegsSaved = 0x03,
            FourRegsSaved = 0x04,
            FiveRegsSaved = 0x05,
            SixRegsSaved = 0x06,
            SevenRegsSaved = 0x07,
            HasStructuredExceptionHandling = 0x08,
            UseEBP = 0x10,
            Reserverd = 0x20,
            FrameFPO = 0x40,
            FrameTrap = 0x80,
            FrameTSS = 0xC0
        }
    }
}
