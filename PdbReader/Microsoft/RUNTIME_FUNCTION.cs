using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PdbReader.Microsoft
{
    /// <summary>Table-based exception handling requires a table entry for all functions
    /// that allocate stack space or call another function (for example, nonleaf functions).
    /// All addresses are image relative, that is, they're 32-bit offsets from the
    /// starting address of the image that contains the function table entry.</summary>
    /// <remarks>See the following URL for exception handling explanation
    /// https://docs.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170</remarks>
    [StructLayout(LayoutKind.Explicit)]
    internal struct RUNTIME_FUNCTION
    {
        [FieldOffset(0)]
        internal uint BeginAddress;
        [FieldOffset(4)]
        internal uint EndAddress;
        // These two fields are an union.
        [FieldOffset(8)]
        internal uint UnwindInfoAddress;
        [FieldOffset(8)]
        internal uint UnwindData;
    }
}
