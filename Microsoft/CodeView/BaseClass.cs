using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class BaseClass : INamedItem
    {
        private _BaseClass _baseClass;
        // unsigned char offset[CV_ZEROLEN];       // variable length offset of base within class
        private ulong _baseClassOffset;

        public string Name => INamedItem.NoName;

        internal static BaseClass Create(PdbStreamReader reader, ref uint maxLength)
        {
            BaseClass result = new BaseClass() {
                _baseClass = reader.Read<_BaseClass>(),
            };
            Utils.SafeDecrement(ref maxLength, _BaseClass.Size);
            uint variantSize;
            result._baseClassOffset = (ulong)reader.ReadVariant(out variantSize);
            Utils.SafeDecrement(ref maxLength, variantSize);
            Utils.SafeDecrement(ref maxLength, reader.HandlePadding(maxLength));
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _BaseClass
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_BaseClass>();
            internal LEAF_ENUM_e leaf;// LF_BCLASS, LF_BINTERFACE
            internal CV_fldattr_t attr; // attribute
            internal uint /*CV_typ_t*/ index; // type index of base class
        }
    }
}
