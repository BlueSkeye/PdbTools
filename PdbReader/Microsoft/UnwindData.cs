using System.Runtime.InteropServices;

namespace PdbReader.Microsoft
{
    internal class UnwindData
    {
        private _UNWIND_INFO _unwindInfo;

        internal byte Flags => (byte)((_unwindInfo.VersionAndFlag & 0xF1) >> 3);

        /// <summary>If nonzero, then the function uses a frame pointer (FP), and this field
        /// is the number of the nonvolatile register used as the frame pointer, using the
        /// same encoding for the operation info field of UNWIND_CODE nodes.</summary>
        internal byte FrameRegister => (byte)(_unwindInfo.FrameRegisterAndOffset & 0x0F);

        /// <summary>If the frame register field is nonzero, this field is the scaled offset
        /// from RSP that is applied to the FP register when it's established. The actual FP
        /// register is set to RSP + 16 * this number, allowing offsets from 0 to 240.
        /// This offset permits pointing the FP register into the middle of the local stack
        /// allocation for dynamic stack frames, allowing better code density through shorter
        /// instructions. (That is, more instructions can use the 8-bit signed offset form.)</summary>
        internal byte FrameOffset => (byte)((_unwindInfo.FrameRegisterAndOffset & 0xF0) >> 4);

        internal byte Version => (byte)(_unwindInfo.VersionAndFlag & 0x07);

        internal static UnwindData Create(PdbStreamReader reader)
        {
            UnwindData result = new UnwindData() {
                _unwindInfo = reader.Read<_UNWIND_INFO>()
            };

            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _UNWIND_INFO
        {
            internal byte VersionAndFlag;
            /// <summary>Length of the function prolog in bytes.</summary>
            internal byte SizeOfProlog;
            /// <summary>The number of slots in the unwind codes array. Some unwind codes,
            /// for example, UWOP_SAVE_NONVOL, require more than one slot in the array.</summary>
            internal byte CountOfCodes;
            internal byte FrameRegisterAndOffset;

            [Flags()]
            internal enum _Flags
            {
                None = 0x00,
                /// <summary>The function has an exception handler that should be called
                /// when looking for functions that need to examine exceptions.</summary>
                UNW_FLAG_EHANDLER = 0x01,
                /// <summary>The function has a termination handler that should be called
                /// when unwinding an exception.</summary>
                UNW_FLAG_UHANDLER = 0x02,
                /// <summary>This unwind info structure is not the primary one for the
                /// procedure. Instead, the chained unwind info entry is the contents of a
                /// previous RUNTIME_FUNCTION entry. For information, see Chained unwind
                /// info structures. If this flag is set, then the UNW_FLAG_EHANDLER and
                /// UNW_FLAG_UHANDLER flags must be cleared. Also, the frame register and
                /// fixed-stack allocation fields must have the same values as in the primary
                /// unwind info.</summary>
                UNW_FLAG_CHAININFO = 0x04
            }
        }
    }
}
