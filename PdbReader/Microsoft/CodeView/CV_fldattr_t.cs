
namespace PdbReader.Microsoft.CodeView
{
    [Flags()]
    internal enum CV_fldattr_t : ushort
    {
        // unsigned short access      :2;     // access protection CV_access_t
        // unsigned short mprop       :3;     // method properties CV_methodprop_t
        PseudoFunction = 0x0020, // compiler generated fcn and does not exist
        NotInheritable = 0x0040, // true if class cannot be inherited
        NonConstructible = 0x0080, // true if class cannot be constructed
        CompilerGenerated = 0x0100, // compiler generated fcn and does exist
        Sealed = 0x0200, // true if method cannot be overridden
    }
}
