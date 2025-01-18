using PdbReader.Microsoft.CodeView;
using PdbReader.Microsoft.CodeView.Types;

namespace PdbReader
{
    /// <summary>The base class for both TPI and IPI streams. Those streams only contains type
    /// records.</summary>
    internal abstract class TypeIndexedStream : IndexedStream
    {
        protected readonly Dictionary<uint, ITypeRecord> _recordByOffset =
            new Dictionary<uint, ITypeRecord>();

        protected TypeIndexedStream(Pdb owner, ushort streamIndex)
            : base(owner, streamIndex)
        {
        }

        /// <remarks>WARNING : This method DOES NOT register the loaded type record against the owning PDB.
        /// This responsibility is left to the caller.</remarks>
        /// <summary></summary>
        /// <param name="recordIndex"></param>
        /// <returns></returns>
        /// <exception cref="BugException"></exception>
        private ITypeRecord LoadLengthPrefixedTypeRecord(ref uint recordIndex)
        {
            // Because it will be modified later and we must keep the original value for use in diagnostic
            // messages.
            uint thisRecordIndex = recordIndex;
            // This is a special case. When no more bytes remain on the block, the first
            // read below will modify the global offset BEFORE reading the first byte.
            // Hence capturing global offset now would provide an erroneous result.
            IStreamGlobalOffset recordStartGlobalOffset = _reader.GetGlobalOffset(true);
            // WARNING : This offset must be captured AFTER the previous call which may have
            // modified the offset value.
            uint recordStartOffset = _reader.Offset;
            // The record length is the total number of bytes for this record EXCLUDING
            // the 2 bytes of the recordLength field itself.
            uint recordLength = _reader.ReadUInt16();
            uint recordTotalLength = (uint)(recordLength + sizeof(ushort));
            IStreamGlobalOffset recordEndGlobalOffsetExcluded = recordStartGlobalOffset.Add(recordTotalLength);
            uint recordEndOffsetExcluded = recordStartOffset + recordTotalLength;
            ITypeRecord result = LoadTypeRecord(ref recordIndex, ref recordLength);
            IStreamGlobalOffset currentGlobalOffset = _reader.GetGlobalOffset();
            uint currentOffset = _reader.Offset;
            if (currentOffset < recordEndOffsetExcluded) {
                uint ignoredBytesCount = recordEndOffsetExcluded - currentOffset;
                bool doNotWarnOnReset = false;
                if (sizeof(uint) <= ignoredBytesCount) {
                    // Emit warning when extra bytes count is greater or equal to doubleword size
                    // or if extra bytes are not explictly allowed.
                    // This should be an incomplete decoding indicator.
                    // NOTICE : This is an heuristic which is not supported by official source
                    // code evidences.
                    Console.WriteLine(
                        $"WARN : Record #{thisRecordIndex} starting at 0x{recordStartGlobalOffset.Value:X8}/{recordStartOffset}.\r\n" +
                        $"Should have ended at 0x{recordEndGlobalOffsetExcluded.Value:X8}/{recordEndOffsetExcluded} : {ignoredBytesCount} bytes ignored.");
                }
                else {
                    doNotWarnOnReset = true;
                    if (_owner.FullDecodingDebugEnabled) {
                        Console.WriteLine($"DBG : {result.Type} record #{thisRecordIndex} fully decoded.");
                    }
                }
                // Adjust reader position.
                _reader.SetGlobalOffset(recordEndGlobalOffsetExcluded, doNotWarnOnReset);
            }
            else if (currentOffset > recordEndOffsetExcluded) {
                uint excessBytesCount = currentOffset - recordEndOffsetExcluded;
                Console.WriteLine(
                    $"WARN : {result.Type} ({result.LeafKind}) record #{thisRecordIndex} starting 0x{recordStartGlobalOffset.Value:X8}/{recordStartOffset}.\r\n" +
                    $"Should have ended at 0x{recordEndGlobalOffsetExcluded.Value:X8}/{recordEndOffsetExcluded} : consumed {excessBytesCount} bytes in excess");
                // Adjust reader position.
                _reader.SetGlobalOffset(recordEndGlobalOffsetExcluded);
            }
            else if (currentOffset == recordEndOffsetExcluded) {
                if (_owner.FullDecodingDebugEnabled) {
                    Console.WriteLine($"DBG : {result.Type} record #{thisRecordIndex} fully decoded.");
                }
            }
            else { throw new BugException(); }
            return result;
        }

        internal ITypeRecord LoadTypeRecord(ref uint recordIndex, ref uint recordLength)
        {
            // Most if not all definitions are from CVINFO.H
            TypeKind recordKind = (TypeKind)_reader.PeekUInt16();
            ITypeRecord result;
            switch (recordKind) {
                case TypeKind.ArgumentList:
                    result = ArgumentList.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Array:
                    result = CodeViewArray.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Array16Bits:
                    result = CodeViewArray16Bits.Create(_reader, ref recordLength);
                    break;
                case TypeKind.BClass:
                    result = BaseClass.Create(_reader, ref recordLength);
                    break;
                case TypeKind.BitField:
                    result = BitField.Create(_reader, ref recordLength);
                    break;
                case TypeKind.BuildInformation:
                    result = BuildInformation.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Class:
                    result = Class.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Enum:
                    result = Enumeration.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Enumerate:
                    result = Enumerate.Create(_reader, ref recordLength);
                    break;
                case TypeKind.FieldList:
                    // WARNING : This is a special case because we create several ITypeRecord at once.
                    // We immediately return and delegate type record registration to the FieldList class.
                    return FieldList.Create(this, ref recordIndex, ref recordLength);
                case TypeKind.FunctionIdentifier:
                    result = FunctionIdentifier.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Index:
                    result = Microsoft.CodeView.Types.Index.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Label:
                    result = Label.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Member:
                    result = Member.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Method:
                    result = Method.Create(_reader, ref recordLength);
                    break;
                case TypeKind.MethodList:
                    result = MethodList.Create(this, ref recordLength);
                    break;
                case TypeKind.MFunction:
                    result = MemberFunction.Create(_reader, ref recordLength);
                    break;
                case TypeKind.MFunctionIdentifier:
                    result = MemberFunctionIdentifier.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Modifier:
                    result = Modifier.Create(_reader, ref recordLength);
                    break;
                case TypeKind.NestedType:
                    result = NestedType.Create(_reader, ref recordLength);
                    break;
                case TypeKind.OneMethod:
                    result = OneMethod.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Pointer:
                    // Remaining bytes may be present that are name chars related.
                    result = PointerBody.Create(_reader, this, ref recordLength);
                    break;
                case TypeKind.Procedure:
                    result = Procedure.Create(_reader, ref recordLength);
                    break;
                case TypeKind.STMember:
                    result = StaticMember.Create(_reader, ref recordLength);
                    break;
                case TypeKind.StringIdentifier:
                    result = StringIdentifier.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Structure:
                    result = Structure.Create(_reader, ref recordLength);
                    break;
                case TypeKind.SubstringList:
                    result = SubstringList.Create(_reader, ref recordLength);
                    break;
                case TypeKind.UDTModuleSourceLine:
                    result = UDTModuleSourceLine.Create(_reader, ref recordLength);
                    break;
                case TypeKind.UDTSourceLine:
                    result = UDTSourceLine.Create(_reader, ref recordLength);
                    break;
                case TypeKind.Union:
                    result = Union.Create(_reader, ref recordLength);
                    break;
                case TypeKind.IVBClass:
                    result = IVirtualBaseClass.Create(_reader, ref recordLength);
                    break;
                case TypeKind.VBClass:
                    result = VirtualBaseClass.Create(_reader, ref recordLength);
                    break;
                case TypeKind.VirtualFunctionTable:
                    result = VirtualFunctionTable.Create(_reader, ref recordLength);
                    break;
                case TypeKind.VFunctionTAB:
                    result = VirtualFunctionTablePointer.Create(_reader, ref recordLength);
                    break;
                case TypeKind.VirtualTableShape:
                    result = VirtualTableShape.Create(_reader, ref recordLength);
                    break;
                default:
                    // TODO : Account for padding pseudo bytes.
                    // Handling should match description from include file (i.e. should only
                    // appear in complex types).
                    string warningMessage = $"WARN : Unknwon type record kind '{recordKind}' / 0x{((int)recordKind):X4}";
                    Console.WriteLine(warningMessage);
                    throw new PDBFormatException(warningMessage);
            }
            _owner.RegisterType(recordIndex++, result);
            return result;
        }

        protected virtual void LoadTypeRecords()
        {
            Console.WriteLine($"Loading {StreamName} stream Type records.");
            uint recordsCount = RecordsCount;
            uint firstInvalidIndex = _header.TypeIndexEndExcluded;
            uint totalRecordBytes = _header.TypeRecordBytes;
            uint offset = 0;
            uint recordIndex = _header.TypeIndexBegin;
            while (offset < totalRecordBytes) {
                if (recordIndex >= firstInvalidIndex) {
                    // We should have consumed the expected total number of bytes.
                    if (offset < totalRecordBytes) {
                        throw new BugException();
                    }
                }
                uint startOffset = _reader.Offset;
                ITypeRecord newRecord = LoadLengthPrefixedTypeRecord(ref recordIndex);
                _recordByOffset.Add(startOffset, newRecord);
                uint deltaOffset = _reader.Offset - startOffset;
                if (0 == deltaOffset) {
                    throw new BugException();
                }
                offset += deltaOffset;
            }
            Console.WriteLine(
                $"{StreamName} records loading completed. {recordIndex - _header.TypeIndexBegin} records found. {recordsCount} were expected.");
            return;
        }
    }
}
