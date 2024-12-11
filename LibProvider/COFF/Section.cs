using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace LibProvider.COFF
{
    internal class Section
    {
        internal readonly IMAGE_SECTION_HEADER Header;

        internal Section(MemoryMappedViewStream from)
        {
            Header = new IMAGE_SECTION_HEADER(from);
            long savedPosition = from.Position;
            try {
                return;
            }
            finally { from.Position = savedPosition; }
        }
    }
}
