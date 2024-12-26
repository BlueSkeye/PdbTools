using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class BuildInformation : ILeafRecord
    {
        internal _InformationBase Base;
        internal uint[] /*CV_ItemId*/ Arguments;

        private BuildInformation(_InformationBase @base)
        {
            Base = @base;
            Arguments = new uint[Base.count];
        }

        public LeafIndices LeafKind => LeafIndices.BuildInformation;

        internal static BuildInformation Create(PdbStreamReader reader,
            ref uint maxLength)
        {
            BuildInformation result = new BuildInformation(reader.Read<_InformationBase>());
            Utils.SafeDecrement(ref maxLength, _InformationBase.Size);
            reader.ReadArray<uint>(result.Arguments, reader.ReadUInt32);
            Utils.SafeDecrement(ref maxLength, ((uint)result.Arguments.Length * sizeof(uint)));
            Utils.SafeDecrement(ref maxLength, reader.HandlePadding(maxLength));
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _InformationBase
        {
            internal static readonly uint Size = (uint)Marshal.SizeOf<_InformationBase>();
            internal LeafIndices leaf; // LF_BUILDINFO
            internal ushort count; // number of arguments
        }
    }
}
