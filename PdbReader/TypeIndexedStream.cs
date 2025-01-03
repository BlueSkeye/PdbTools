using PdbReader.Microsoft.CodeView;
using PdbReader.Microsoft.CodeView.Types;

namespace PdbReader
{
    /// <summary>The base class for both TPI and IPI streams. Those streams only contains type records.</summary>
    internal abstract class TypeIndexedStream : IndexedStream
    {
        protected TypeIndexedStream(Pdb owner, ushort streamIndex)
            : base(owner, streamIndex)
        {
        }

        private ITypeRecord LoadLengthPrefixedTypeRecord(uint recordIdentifier)
        {
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
            ITypeRecord result = LoadTypeRecord(ref recordLength);
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
                        $"WARN : Record #{recordIdentifier} starting at 0x{recordStartGlobalOffset.Value:X8}/{recordStartOffset}.\r\n" +
                        $"Should have ended at 0x{recordEndGlobalOffsetExcluded.Value:X8}/{recordEndOffsetExcluded} : {ignoredBytesCount} bytes ignored.");
                }
                else {
                    doNotWarnOnReset = true;
                    if (_owner.FullDecodingDebugEnabled) {
                        Console.WriteLine($"DBG : {result.Type} record #{recordIdentifier} fully decoded.");
                    }
                }
                // Adjust reader position.
                _reader.SetGlobalOffset(recordEndGlobalOffsetExcluded, doNotWarnOnReset);
            }
            else if (currentOffset > recordEndOffsetExcluded) {
                uint excessBytesCount = currentOffset - recordEndOffsetExcluded;
                Console.WriteLine(
                    $"WARN : {result.Type} ({result.LeafKind}) record #{recordIdentifier} starting 0x{recordStartGlobalOffset.Value:X8}/{recordStartOffset}.\r\n" +
                    $"Should have ended at 0x{recordEndGlobalOffsetExcluded.Value:X8}/{recordEndOffsetExcluded} : consumed {excessBytesCount} bytes in excess");
                // Adjust reader position.
                _reader.SetGlobalOffset(recordEndGlobalOffsetExcluded);
            }
            else if (currentOffset == recordEndOffsetExcluded) {
                if (_owner.FullDecodingDebugEnabled) {
                    Console.WriteLine($"DBG : {result.Type} record #{recordIdentifier} fully decoded.");
                }
            }
            else { throw new BugException(); }
            return result;
        }

        internal ITypeRecord LoadTypeRecord(ref uint recordLength)
        {
            // Most if not all definitions are from CVINFO.H
            TypeKind recordKind = (TypeKind)_reader.PeekUInt16();
            switch (recordKind) {
                case TypeKind.ArgumentList:
                    return ArgumentList.Create(_reader, ref recordLength);
                case TypeKind.Array:
                    return CodeViewArray.Create(_reader, ref recordLength);
                case TypeKind.Array16Bits:
                    return CodeViewArray16Bits.Create(_reader, ref recordLength);
                case TypeKind.BClass:
                    return BaseClass.Create(_reader, ref recordLength);
                case TypeKind.BitField:
                    return BitField.Create(_reader, ref recordLength);
                case TypeKind.BuildInformation:
                    return BuildInformation.Create(_reader, ref recordLength);
                case TypeKind.Class:
                    return Class.Create(_reader, ref recordLength);
                case TypeKind.Enum:
                    return Enumeration.Create(_reader, ref recordLength);
                case TypeKind.Enumerate:
                    return Enumerate.Create(_reader, ref recordLength);
                case TypeKind.FieldList:
                    return FieldList.Create(this, ref recordLength);
                case TypeKind.FunctionIdentifier:
                    return FunctionIdentifier.Create(_reader, ref recordLength);
                case TypeKind.Index:
                    return Microsoft.CodeView.Types.Index.Create(_reader, ref recordLength);
                case TypeKind.Label:
                    return Label.Create(_reader, ref recordLength);
                case TypeKind.Member:
                    return Member.Create(_reader, ref recordLength);
                case TypeKind.Method:
                    return Method.Create(_reader, ref recordLength);
                case TypeKind.MethodList:
                    return MethodList.Create(this, ref recordLength);
                case TypeKind.MFunction:
                    return MemberFunction.Create(_reader, ref recordLength);
                case TypeKind.MFunctionIdentifier:
                    return MemberFunctionIdentifier.Create(_reader, ref recordLength);
                case TypeKind.Modifier:
                    return Modifier.Create(_reader, ref recordLength);
                case TypeKind.NestedType:
                    return NestedType.Create(_reader, ref recordLength);
                case TypeKind.OneMethod:
                    return OneMethod.Create(_reader, ref recordLength);
                case TypeKind.Pointer:
                    // Remaining bytes may be present that are name chars related.
                    return PointerBody.Create(_reader, this, ref recordLength);
                case TypeKind.Procedure:
                    return Procedure.Create(_reader, ref recordLength);
                case TypeKind.STMember:
                    return StaticMember.Create(_reader, ref recordLength);
                case TypeKind.StringIdentifier:
                    return StringIdentifier.Create(_reader, ref recordLength);
                case TypeKind.Structure:
                    return Structure.Create(_reader, ref recordLength);
                case TypeKind.SubstringList:
                    return SubstringList.Create(_reader, ref recordLength);
                case TypeKind.UDTModuleSourceLine:
                    return UDTModuleSourceLine.Create(_reader, ref recordLength);
                case TypeKind.UDTSourceLine:
                    return UDTSourceLine.Create(_reader, ref recordLength);
                case TypeKind.Union:
                    return Union.Create(_reader, ref recordLength);
                case TypeKind.IVBClass:
                    return IVirtualBaseClass.Create(_reader, ref recordLength);
                case TypeKind.VBClass:
                    return VirtualBaseClass.Create(_reader, ref recordLength);
                case TypeKind.VirtualFunctionTable:
                    return VirtualFunctionTable.Create(_reader, ref recordLength);
                case TypeKind.VFunctionTAB:
                    return VirtualFunctionTablePointer.Create(_reader, ref recordLength);
                case TypeKind.VirtualTableShape:
                    return VirtualTableShape.Create(_reader, ref recordLength);
                default:
                    // TODO : Account for padding pseudo bytes.
                    // Handling should match description from include file (i.e. should only
                    // appear in complex types).
                    string warningMessage = $"WARN : Unknwon type record kind '{recordKind}' / 0x{((int)recordKind):X4}";
                    Console.WriteLine(warningMessage);
                    throw new PDBFormatException(warningMessage);
            }
        }

        protected virtual void LoadTypeRecords()
        {
            Console.WriteLine($"Loading {StreamName} stream Type records.");
            uint recordsCount = RecordsCount;
            uint totalRecordBytes = _header.TypeRecordBytes;
            uint offset = 0;
            uint recordIndex = 0;
            while (offset < totalRecordBytes) {
                uint startOffset = _reader.Offset;
                ITypeRecord newRecord = LoadLengthPrefixedTypeRecord(recordIndex);
                // TODO : Should store the returned record.
                uint deltaOffset = _reader.Offset - startOffset;
                if (0 == deltaOffset) {
                    throw new BugException();
                }
                offset += deltaOffset;
                if (++recordIndex >= recordsCount) {
                    // We should have consumed the expected total number of bytes.
                    if (offset < totalRecordBytes) {
                        throw new BugException();
                    }
                }
            }
            Console.WriteLine(
                $"{StreamName} records loading completed. {recordIndex} records found. {recordsCount} were expected.");
            return;
        }
    }
}
