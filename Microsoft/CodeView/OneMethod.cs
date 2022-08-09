using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class OneMethod : INamedItem
    {
        private _OneMethod _oneMethod;
        // offset in vfunctable if intro virtual followed by length prefixed name of method
        // unsigned long vbaseoff[CV_ZEROLEN];
        private uint _virtualFunctionTableOffset;

        public string Name { get; private set; }

        internal static OneMethod Create(PdbStreamReader reader)
        {
            OneMethod result = new OneMethod() {
                _oneMethod = reader.Read<_OneMethod>(),
            };
            CV_methodprop_e methodProperties = Utils.GetMethodProperties(result._oneMethod.attr);
            switch (methodProperties) {
                case CV_methodprop_e.Introduction:
                case CV_methodprop_e.PureIntroduction:
                    result._virtualFunctionTableOffset = reader.ReadUInt32();
                    break;
                default:
                    break;
            }
            result.Name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _OneMethod
        {
            internal LEAF_ENUM_e leaf; // LF_ONEMETHOD
            internal CV_fldattr_t attr; // method attribute
            internal uint /*CV_typ_t*/ index; // index to type record for procedure
        }
    }
}
