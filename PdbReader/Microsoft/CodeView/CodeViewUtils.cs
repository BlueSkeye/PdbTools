
using PdbReader.Microsoft.CodeView.Enumerations;
using PdbReader.Microsoft.CodeView.Types;

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

        internal static bool IsBuiltinType(TypeKind candidate)
        {
            return (0 != (0x8000 & (ushort)candidate));
        }

        internal static bool IsValidBuiltinType(TypeKind candidate)
        {
            if (!IsBuiltinType(candidate)) {
                return false;
            }
            switch (candidate) {
                case TypeKind.Character:
                case TypeKind.Short:
                case TypeKind.UnsignedShort:
                case TypeKind.Integer:
                case TypeKind.UnsignedInteger:
                case TypeKind.Real32Bits:
                case TypeKind.Real64Bits:
                case TypeKind.Real80Bits:
                case TypeKind.Real128Bits:
                case TypeKind.LongInteger:
                case TypeKind.UnsignedLongInteger:
                case TypeKind.Real48Bits:
                case TypeKind.Complex32Bits:
                case TypeKind.Complex64Bits:
                case TypeKind.Complex80Bits:
                case TypeKind.Complex128Bits:
                case TypeKind.VariableLengthString:
                case TypeKind.OctalWord:
                case TypeKind.UnsignedOctalWord:
                case TypeKind.Decimal:
                case TypeKind.Date:
                case TypeKind.UTF8String:
                case TypeKind.Real16Bits:
                    return true;
                default:
                    return false;
            }
        }
    }
}
