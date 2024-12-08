
namespace PdbReader.Microsoft.CodeView
{
    [Flags()]
    internal enum CV_funcattr_t : byte
    {
        cxxreturnudt = 0x01, // true if C++ style ReturnUDT
        ctor = 0x02, // true if func is an instance constructor
        ctorvbase = 0x04 // true if func is an instance constructor of a class with virtual bases
    }
}
