
namespace PdbReader.Microsoft.CodeView
{
    internal static class CodeViewUtils
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

        internal static bool IsBuiltinType(LeafIndices candidate)
        {
            return (0 != (0x8000 & (ushort)candidate));
        }

        internal static bool IsValidBuiltinType(LeafIndices candidate)
        {
            if (!IsBuiltinType(candidate)) {
                return false;
            }
            switch (candidate) {
                case LeafIndices.Character:
                case LeafIndices.Short:
                case LeafIndices.UnsignedShort:
                case LeafIndices.Integer:
                case LeafIndices.UnsignedInteger:
                case LeafIndices.Real32Bits:
                case LeafIndices.Real64Bits:
                case LeafIndices.Real80Bits:
                case LeafIndices.Real128Bits:
                case LeafIndices.LongInteger:
                case LeafIndices.UnsignedLongInteger:
                case LeafIndices.Real48Bits:
                case LeafIndices.Complex32Bits:
                case LeafIndices.Complex64Bits:
                case LeafIndices.Complex80Bits:
                case LeafIndices.Complex128Bits:
                case LeafIndices.VariableLengthString:
                case LeafIndices.OctalWord:
                case LeafIndices.UnsignedOctalWord:
                case LeafIndices.Decimal:
                case LeafIndices.Date:
                case LeafIndices.UTF8String:
                case LeafIndices.Real16Bits:
                    return true;
                default:
                    return false;
            }
        }
    }
}
