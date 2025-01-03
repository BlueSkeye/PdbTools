using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView.Types
{
    internal class VirtualFunctionTable : TypeRecord
    {
        internal _VirtualFunctionTable _data;
        //unsigned char Names[1]; // array of names.
        // The first is the name of the vtable.
        // The others are the names of the methods.
        // TS-TODO: replace a name with a NamedCodeItem once Weiping is done, to
        // avoid duplication of method names.

        internal List<string> _names;

        public override TypeKind LeafKind => TypeKind.VirtualFunctionTable;

        private VirtualFunctionTable(_VirtualFunctionTable data)
        {
            _data = data;
            _names = new List<string>();
        }

        internal static VirtualFunctionTable Create(PdbStreamReader reader,
            ref uint maxLength)
        {
            _VirtualFunctionTable data = reader.Read<_VirtualFunctionTable>();
            Utils.SafeDecrement(ref maxLength, _VirtualFunctionTable.Size);
            VirtualFunctionTable result = new VirtualFunctionTable(data);
            // Some virtual table shapes appear to have padding bytes.
            Utils.SafeDecrement(ref maxLength, reader.HandlePadding(maxLength));
            uint namesLength = data.len;
            while (0 < namesLength)
            {
                string name = reader.ReadNTBString(ref namesLength);
                result._names.Add(name);
            }
            Utils.SafeDecrement(ref maxLength, namesLength);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _VirtualFunctionTable
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_VirtualFunctionTable>();
            internal TypeKind leaf; // LF_VFTABLE
            internal uint /*CV_typ_t*/ type; // class/structure that owns the vftable
            internal uint /*CV_typ_t*/ baseVftable; // vftable from which this vftable is derived
            internal uint offsetInObjectLayout; // offset of the vfptr to this table, relative to the start of the object layout.
            internal uint len; // length of the Names array below in bytes.
        }
    }
}
