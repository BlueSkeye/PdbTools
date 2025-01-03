﻿using PdbReader.Microsoft.CodeView.Enumerations;
using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class MethodList : TypeRecord
    {
        internal TypeKind _leaf; // LF_METHODLIST
        // char data[CV_ZEROLEN]; // field list sub lists
        internal List<ListedMethod> _members = new List<ListedMethod>();

        private MethodList(TypeKind leaf)
        {
            _leaf = leaf;
        }

        public override TypeKind LeafKind => TypeKind.MethodList;

        internal static MethodList Create(IndexedStream stream, ref uint maxLength)
        {
            PdbStreamReader reader = stream._reader;
            uint endOffsetExcluded = maxLength + reader.Offset;
            MethodList result = new MethodList((TypeKind)reader.ReadUInt16());
            Utils.SafeDecrement(ref maxLength, sizeof(ushort));
            while (endOffsetExcluded > reader.Offset)
            {
                result._members.Add(ListedMethod.Create(reader, ref maxLength));
            }
            return result;
        }

        internal class ListedMethod
        {
            private _Method _method;
            // unsigned long vbaseoff[CV_ZEROLEN];    // offset in vfunctable if intro virtual
            private uint _virtualFunctionTableOffset;

            internal static ListedMethod Create(PdbStreamReader reader, ref uint maxLength)
            {
                ListedMethod result = new ListedMethod()
                {
                    _method = reader.Read<_Method>()
                };
                Utils.SafeDecrement(ref maxLength, _Method.Size);
                CV_methodprop_e methodProperties = CodeViewUtils.GetMethodProperties(result._method.attr);
                switch (methodProperties)
                {
                    case CV_methodprop_e.PureIntroduction:
                    case CV_methodprop_e.Introduction:
                        result._virtualFunctionTableOffset = reader.ReadUInt32();
                        Utils.SafeDecrement(ref maxLength, sizeof(uint));
                        break;
                    default:
                        break;
                }
                return result;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct _Method
            {
                internal static readonly uint Size = (uint)Marshal.SizeOf<_Method>();
                internal CV_fldattr_t attr; // method attribute
                internal ushort Pad0; // internal padding, must be 0
                internal uint /*CV_typ_t*/ index; // index to type record for procedure
            }
        }
    }
}
