using System.Runtime.InteropServices;

using PdbReader;

namespace PdbDownloader
{
    internal class RVAReaderWriter
    {
        private readonly IntPtr _baseAddress;
        private readonly IntPtr _nativeBuffer;
        private readonly uint _nativeBufferSize;

        internal RVAReaderWriter(IntPtr nativeBuffer, uint nativeBufferSize,
            IntPtr baseAddress)
        {
            _baseAddress = baseAddress;
            _nativeBuffer = nativeBuffer;
            _nativeBufferSize = nativeBufferSize;
        }

        internal void Copy(BinaryReader from, ulong relativeVirtualAddress,
            uint size)
        {
            if (0 == size) {
                throw new ArgumentException(nameof(size));
            }
            int intSize = Downloader.SafeCastUintToInt(size);
            byte[] rawData = new byte[intSize];

            if (Downloader.SafeCastULongToInt(relativeVirtualAddress + size - 1) > _nativeBufferSize) {
                throw new ArgumentException();
            }
            int readSize = from.Read(rawData, 0, intSize);
            if (intSize != readSize) {
                throw new BugException("Not enough data.");
            }
            Marshal.Copy(rawData, 0, IntPtr.Add(_nativeBuffer,
                Downloader.SafeCastULongToInt(relativeVirtualAddress)), intSize);
        }

        internal IMAGE_SECTION_HEADER FindSection(Downloader context,
            uint relativeVirtualAddress)
        {
            return context._sections[FindSectionIndex(context, relativeVirtualAddress)];
        }

        internal int FindSectionIndex(Downloader context, uint relativeVirtualAddress)
        {
            int sectionsCount = context._sections.Length;
            for (int index = 0; index < sectionsCount; index++) {
                IMAGE_SECTION_HEADER candidate = context._sections[index];
                uint sectionRelativeStartAddress = candidate.virtualAddress;
                if (1 == sectionRelativeStartAddress.CompareTo(relativeVirtualAddress)) {
                    continue;
                }
                uint sectionEndAddress = Downloader.SafeCastIntToUint(
                    Downloader.SafeCastLongToInt(sectionRelativeStartAddress +
                        Downloader.SafeCastUintToInt(candidate.virtualSize - 1)));
                if (0 <= sectionEndAddress.CompareTo(relativeVirtualAddress)) {
                    return index;
                }
            }
            throw new ApplicationException(
                $"BUG : Section not found for RVA 0x{relativeVirtualAddress:X8}");
        }
    }
}
