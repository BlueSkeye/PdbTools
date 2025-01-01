using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal class Procedure : TypeRecord
    {
        internal static readonly uint Size = (uint)Marshal.SizeOf<Procedure>();
        internal LeafIndices leaf; // LF_PROCEDURE
        internal uint /*CV_typ_t*/ rvtype; // type index of return value
        internal CV_call_e calltype; // calling convention (CV_call_t)
        internal CV_funcattr_t funcattr; // attributes
        internal ushort parmcount; // number of parameters
        internal uint /*CV_typ_t*/ arglist;        // type index of argument list

        public override LeafIndices LeafKind => LeafIndices.Procedure;

        internal static Procedure Create(PdbStreamReader reader, ref uint maxLength)
        {
            Procedure result = new Procedure() {
                leaf = (LeafIndices)reader.ReadUInt16(),
                rvtype = reader.ReadUInt32(),
                calltype = (CV_call_e)reader.ReadByte(),
                funcattr = (CV_funcattr_t)reader.ReadByte(),
                parmcount = reader.ReadUInt16(),
                arglist = reader.ReadUInt32()
            };
            return result;
        }
    }
}
