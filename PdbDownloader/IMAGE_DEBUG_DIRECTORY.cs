using System.Runtime.InteropServices;

namespace PdbDownloader
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_DEBUG_DIRECTORY
    {
        /// <summary>Reserved</summary>
        [FieldOffset(0)]
        internal uint Characteristics;
        /// <summary>The ime and date the debugging information was created.</summary>
        [FieldOffset(4)]
        internal uint TimeDateStamp;
        /// <summary>The major version number of the debugging information format.</summary>
        [FieldOffset(8)]
        internal ushort MajorVersion;
        /// <summary>The minor version number of the debugging information format.</summary>
        [FieldOffset(10)]
        internal ushort MinorVersion;
        /// <summary>The format of the debugging information. This member can be
        /// one of the following values.</summary>
        [FieldOffset(12)]
        internal DebuggingInformationType Type;
        /// <summary>The size of the debugging information, in bytes. This value
        /// does not include the debug directory itself.</summary>
        [FieldOffset(16)]
        internal uint SizeOfData;
        /// <summary>The address of the debugging information when the image is
        /// loaded, relative to the image base.</summary>
        [FieldOffset(20)]
        internal uint AddressOfRawData;
        /// <summary>A file pointer to the debugging information.</summary>
        [FieldOffset(24)]
        internal uint PointerToRawData;

        internal enum DebuggingInformationType : uint
        {
            /// <summary>Unknown value, ignored by all tools.</summary>
            Unknown = 0,
            /// <summary>COFF debugging information (line numbers, symbol table,
            /// and string table). This type of debugging information is also
            /// pointed to by fields in the file headers.</summary>
            Coff = 1,
            /// <summary>CodeView debugging information.The format of the data
            /// block is described by the CodeView 4.0 specification.</summary>
            Codeview = 2,
            /// <summary>Frame pointer omission(FPO) information. This
            /// information tells the debugger how to interpret nonstandard stack
            /// frames, which use the EBP register for a purpose other than as a
            /// frame pointer.</summary>
            FramePointerOmission = 3,
            /// <summary>Miscellaneous information.</summary>
            Miscleanous = 4,
            /// <summary>Exception information.</summary>
            Debug = 5,
            /// <summary>Fixup information.</summary>
            TypeFixup = 6,
            /// <summary>Borland debugging information.</summary>
            Borland = 9
        }
    }
}
