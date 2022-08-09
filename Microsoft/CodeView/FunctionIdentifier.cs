using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class FunctionIdentifier
    {
        internal _FunctionIdentifier Identifier { get; private set; }
        internal string Name { get; private set; }

        internal static FunctionIdentifier Create(PdbStreamReader reader)
        {
            FunctionIdentifier result = new FunctionIdentifier() {
                Identifier = reader.Read<_FunctionIdentifier>()
            };
            result.Name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _FunctionIdentifier
        {
            internal LEAF_ENUM_e leaf; // LF_FUNC_ID
            internal uint /*CV_ItemId*/ scopeId; // parent scope of the ID, 0 if global
            internal uint /*CV_typ_t*/ type; // function type
        }
    }
}
