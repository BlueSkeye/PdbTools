using PdbReader.Microsoft.CodeView.Enumerations;
using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal abstract class VirtualBaseClassBase : TypeRecord
    {
        private _VirtualBaseClass _virtualBaseClass;
        // byte vbpoff[CV_ZEROLEN];
        // virtual base pointer offset from address point
        // followed by virtual base offset from vbtable
        private List<Entry> _entries;

        protected delegate VirtualBaseClassBase Instanciator(_VirtualBaseClass baseClass);

        protected VirtualBaseClassBase(_VirtualBaseClass baseClass)
        {
            _virtualBaseClass = baseClass;
            _entries = new List<Entry>();
        }

        protected static VirtualBaseClassBase Create(PdbStreamReader reader, ref uint maxLength,
            Instanciator instanciator)
        {
            VirtualBaseClassBase result = instanciator(reader.Read<_VirtualBaseClass>());
            Utils.SafeDecrement(ref maxLength, _VirtualBaseClass.Size);
            while (0 < maxLength)
            {
                result._entries.Add(Entry.Create(reader, ref maxLength));
            }
            return result;
        }

        public string Name => INamedItem.NoName;

        internal class Entry
        {
            private _PointerOffsetPair _pointerAndOffset;
            private string _name;

            internal static Entry Create(PdbStreamReader reader, ref uint maxLength)
            {
                Entry result = new Entry()
                {
                    _pointerAndOffset = reader.Read<_PointerOffsetPair>()
                };
                Utils.SafeDecrement(ref maxLength, _PointerOffsetPair.Size);
                result._name = reader.ReadNTBString(ref maxLength);
                return result;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct _PointerOffsetPair
            {
                internal static readonly uint Size = (uint)Marshal.SizeOf<_PointerOffsetPair>();
                internal uint _basePointer;
                internal uint _baseOffset;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _VirtualBaseClass
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_VirtualBaseClass>();
            internal TypeKind leaf; // LF_VBCLASS, LV_IVBCLASS
            internal CV_fldattr_t attr; // attribute
            internal uint /*CV_typ_t*/ index; // type index of direct virtual base class
            internal uint /*CV_typ_t*/ vbptr; // type index of virtual base pointer
        }
    }
}
