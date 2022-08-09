using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class MethodList
    {
        internal LEAF_ENUM_e _leaf; // LF_METHODLIST
        // char data[CV_ZEROLEN]; // field list sub lists
        internal List<ListedMethod> _members = new List<ListedMethod>();

        private MethodList(LEAF_ENUM_e leaf)
        {
            _leaf = leaf;
        }

        internal static MethodList Create(IndexedStream stream, uint recordLength)
        {
            PdbStreamReader reader = stream._reader;
            uint endOffsetExcluded = recordLength + reader.Offset;
            MethodList result = new MethodList((LEAF_ENUM_e)reader.ReadUInt16());
            while (endOffsetExcluded > reader.Offset) {
                result._members.Add(ListedMethod.Create(reader));
            }
            return result;
        }

        internal class ListedMethod
        {
            private _Method _method;
            // unsigned long vbaseoff[CV_ZEROLEN];    // offset in vfunctable if intro virtual
            private uint _virtualFunctionTableOffset;

            internal static ListedMethod Create(PdbStreamReader reader)
            {
                ListedMethod result = new ListedMethod() {
                    _method = reader.Read<_Method>()
                };
                if (CV_methodprop_e.PureIntroduction == Utils.GetMethodProperties(result._method.attr)) {
                    result._virtualFunctionTableOffset = reader.ReadUInt32();
                }
                return result;
            }
            
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct _Method
            {
                internal CV_fldattr_t attr; // method attribute
                internal ushort Pad0; // internal padding, must be 0
                internal uint /*CV_typ_t*/ index; // index to type record for procedure
            }
        }
    }
}
