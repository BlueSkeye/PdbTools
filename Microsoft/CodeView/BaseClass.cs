using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class BaseClass : INamedItem
    {
        private _BaseClass _baseClass;
        // unsigned char offset[CV_ZEROLEN];       // variable length offset of base within class
        private ulong _baseClassOffset;

        public string Name => INamedItem.NoName;

        internal static BaseClass Create(PdbStreamReader reader)
        {
            BaseClass result = new BaseClass() {
                _baseClass = reader.Read<_BaseClass>(),
            };
            result._baseClassOffset = reader.ReadVariableLengthValue();
            reader.HandlePadding();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _BaseClass
        {
            internal LEAF_ENUM_e leaf;// LF_BCLASS, LF_BINTERFACE
            internal CV_fldattr_t attr; // attribute
            internal uint /*CV_typ_t*/ index; // type index of base class
        }
    }
}
