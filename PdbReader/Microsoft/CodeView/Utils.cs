
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

        internal static void SafeDecrement(ref uint value, uint decrementBy)
        {
            if (value < decrementBy) {
                throw new BugException();
            }
            value -= decrementBy;
        }
        
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

        internal static bool IsBuiltinType(LEAF_ENUM_e candidate)
        {
            return (0 != (0x8000 & (ushort)candidate));
        }

        internal static bool IsValidBuiltinType(LEAF_ENUM_e candidate)
        {
            if (!IsBuiltinType(candidate)) {
                return false;
            }
            switch (candidate) {
                case LEAF_ENUM_e.Character:
                case LEAF_ENUM_e.Short:
                case LEAF_ENUM_e.UnsignedShort:
                case LEAF_ENUM_e.Integer:
                case LEAF_ENUM_e.UnsignedInteger:
                case LEAF_ENUM_e.Real32Bits:
                case LEAF_ENUM_e.Real64Bits:
                case LEAF_ENUM_e.Real80Bits:
                case LEAF_ENUM_e.Real128Bits:
                case LEAF_ENUM_e.LongInteger:
                case LEAF_ENUM_e.UnsignedLongInteger:
                case LEAF_ENUM_e.Real48Bits:
                case LEAF_ENUM_e.Complex32Bits:
                case LEAF_ENUM_e.Complex64Bits:
                case LEAF_ENUM_e.Complex80Bits:
                case LEAF_ENUM_e.Complex128Bits:
                case LEAF_ENUM_e.VariableLengthString:
                case LEAF_ENUM_e.OctalWord:
                case LEAF_ENUM_e.UnsignedOctalWord:
                case LEAF_ENUM_e.Decimal:
                case LEAF_ENUM_e.Date:
                case LEAF_ENUM_e.UTF8String:
                case LEAF_ENUM_e.Real16Bits:
                    return true;
                default:
                    return false;
            }
        }
    }
}
