using System.Runtime.InteropServices;

namespace PdbDownloader
{
    [StructLayout(LayoutKind.Explicit)]
    internal class IMAGE_DOS_HEADER
    {
        [FieldOffset(0x00)]
        internal short e_magic;                     // Magic number
        [FieldOffset(0x02)]
        internal short e_cblp;                      // Bytes on last page of file
        [FieldOffset(0x04)]
        internal short e_cp;                        // Pages in file
        [FieldOffset(0x06)]
        internal short e_crlc;                      // Relocations
        [FieldOffset(0x08)]
        internal short e_cparhdr;                   // Size of header in paragraphs
        [FieldOffset(0x0A)]
        internal short e_minalloc;                  // Minimum extra paragraphs needed
        [FieldOffset(0x0C)]
        internal short e_maxalloc;                  // Maximum extra paragraphs needed
        [FieldOffset(0x0E)]
        internal short e_ss;                        // Initial (relative) SS value
        [FieldOffset(0x10)]
        internal short e_sp;                        // Initial SP value
        [FieldOffset(0x12)]
        internal short e_csum;                      // Checksum
        [FieldOffset(0x14)]
        internal short e_ip;                        // Initial IP value
        [FieldOffset(0x16)]
        internal short e_cs;                        // Initial (relative) CS value
        [FieldOffset(0x18)]
        internal short e_lfarlc;                    // File address of relocation table
        [FieldOffset(0x1A)]
        internal short e_ovno;                      // Overlay number
        [FieldOffset(0x1C)]
        internal short e_res_0;                    // Reserved words
        [FieldOffset(0x1E)]
        internal short e_res_1;                    // Reserved words
        [FieldOffset(0x20)]
        internal short e_res_2;                    // Reserved words
        [FieldOffset(0x22)]
        internal short e_res_3;                    // Reserved words
        [FieldOffset(0x24)]
        internal short e_oemid;                     // OEM identifier (for e_oeminfo)
        [FieldOffset(0x26)]
        internal short e_oeminfo;                   // OEM information; e_oemid specific
        [FieldOffset(0x28)]
        internal short e_res2_0;                  // Reserved words
        [FieldOffset(0x2A)]
        internal short e_res2_1;                  // Reserved words
        [FieldOffset(0x2C)]
        internal short e_res2_2;                  // Reserved words
        [FieldOffset(0x2E)]
        internal short e_res2_3;                  // Reserved words
        [FieldOffset(0x30)]
        internal short e_res2_4;                  // Reserved words
        [FieldOffset(0x32)]
        internal short e_res2_5;                  // Reserved words
        [FieldOffset(0x34)]
        internal short e_res2_6;                  // Reserved words
        [FieldOffset(0x36)]
        internal short e_res2_7;                  // Reserved words
        [FieldOffset(0x38)]
        internal short e_res2_8;                  // Reserved words
        [FieldOffset(0x3A)]
        internal short e_res2_9;                  // Reserved words
        [FieldOffset(0x3C)]
        internal int e_lfanew;                    // File address of new exe header
    }
}
