
namespace PdbReader.Microsoft.CodeView
{
    internal static class Utils
    {
        private const ushort MethodPropertiesMask = 0x001C;
        private const ushort MethodPropertiesShift = 2;

        private const uint PointerModeMask = 0x000000E0;
        private const ushort PointerModeShift = 5;

        private const uint PointerSizeMask = 0x0007E000;
        private const ushort PointerSizeShift = 13;

        private const uint PointerTypeMask = 0x0000001F;
        private const ushort PointerTypeShift = 0;

        internal static CV_methodprop_e GetMethodProperties(CV_fldattr_t attributes)
        {
            return (CV_methodprop_e)(((ushort)attributes & MethodPropertiesMask) >> MethodPropertiesShift);
        }

        internal static CV_ptrmode_e GetPointerMode(PointerBody.Attributes attributes)
        {
            return (CV_ptrmode_e)(((uint)attributes & PointerModeMask) >> PointerModeShift);
        }

        internal static ushort GetPointerSize(PointerBody.Attributes attributes)
        {
            return (ushort)(((uint)attributes & PointerSizeMask) >> PointerSizeShift);
        }

        internal static CV_ptrtype_e GetPointerType(PointerBody.Attributes attributes)
        {
            return (CV_ptrtype_e)(((uint)attributes & PointerTypeMask) >> PointerTypeShift);
        }
    }
}
