using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class OneMethod : TypeRecord, INamedItem
    {
        private _OneMethod _oneMethod;
        // offset in vfunctable if intro virtual followed by length prefixed name of method
        // unsigned long vbaseoff[CV_ZEROLEN];
        private uint _virtualFunctionTableOffset;

        public override LeafIndices LeafKind => LeafIndices.OneMethod;

        public string Name { get; private set; }

        internal static OneMethod Create(PdbStreamReader reader, ref uint maxLength)
        {
            OneMethod result = new OneMethod() {
                _oneMethod = reader.Read<_OneMethod>(),
            };
            Utils.SafeDecrement(ref maxLength, _OneMethod.Size);
            CV_methodprop_e methodProperties = CodeViewUtils.GetMethodProperties(result._oneMethod.attr);
            switch (methodProperties) {
                case CV_methodprop_e.Introduction:
                case CV_methodprop_e.PureIntroduction:
                    result._virtualFunctionTableOffset = reader.ReadUInt32();
                    Utils.SafeDecrement(ref maxLength, sizeof(uint));
                    break;
                default:
                    break;
            }
            result.Name = reader.ReadNTBString(ref maxLength);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _OneMethod
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_OneMethod>();
            internal LeafIndices leaf; // LF_ONEMETHOD
            internal CV_fldattr_t attr; // method attribute
            internal uint /*CV_typ_t*/ index; // index to type record for procedure
        }
    }
}
