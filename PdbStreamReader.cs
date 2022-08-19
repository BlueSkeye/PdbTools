using System.Runtime.InteropServices;
using System.Text;

using PdbReader.Microsoft.CodeView;

namespace PdbReader
{
    internal class PdbStreamReader
    {
        internal delegate T ReadDelegate<T>();

        private readonly uint[] _blocks;
        private readonly uint _blockSize;
        /// <summary>Index within <see cref="_blocks"/> of current block.
        /// WARNING : Never set this field value. Use CurrentBlockIndex setter instead.
        /// </summary>
        private int _currentBlockIndex;
        /// <summary>For optimization purpose, always equal to _blocks[_currentBlockIndex]
        /// </summary>
        private uint _currentBlockNumber;
        /// <summary>Index within current block of first unread byte.</summary>
        private uint _currentBlockOffset;
        private bool _endOfStreamReached = false;
        private readonly Pdb _pdb;
        private readonly uint _streamSize;

        internal PdbStreamReader(Pdb owner, uint streamIndex)
        {
            if (null == owner) { throw new ArgumentNullException(nameof(owner)); }
            _pdb = owner;
            _blocks = owner.GetStreamMap(streamIndex, out _streamSize);
            _blockSize = _pdb.SuperBlock.BlockSize;
            SetPosition(0, 0);
        }

        /// <summary>Returns the current offset within the stream this reader is bound to.
        /// </summary>
        internal uint Offset
        {
            get {
                ulong result = ((uint)_currentBlockIndex * _blockSize) + _currentBlockOffset;
                if (uint.MaxValue < result) {
                    throw new OverflowException();
                }
                return (uint)result;
            }
            set
            {
                // TODO : Check stream size against value.
                uint currentBlockIndex = (value / _blockSize);
                if (int.MaxValue < currentBlockIndex) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                SetPosition(currentBlockIndex, value % _blockSize);
            }
        }

        /// <summary>Get number of bytes not yet read within current block.</summary>
        internal uint RemainingBlockBytes
        {
            get
            {
                if (_blockSize < _currentBlockOffset) {
                    throw new BugException();
                }
                return _blockSize - _currentBlockOffset;
            }
        }

        internal uint StreamSize => _streamSize;

        private void AssertNotEndOfStream()
        {
            if (_endOfStreamReached) {
                throw new BugException();
            }
        }

        private uint ComputePaddingSize(uint boundarySize)
        {
            return (boundarySize - (_currentBlockOffset % boundarySize))
                % boundarySize;
        }

        internal IStreamGlobalOffset GetGlobalOffset(bool ensureAtLeastOneAvailableByte = false)
        {
            return new GlobalOffset(this, _GetGlobalOffset(ensureAtLeastOneAvailableByte));
        }

        /// <summary>Get the current global offset for this stream in the underlying file.
        /// This version is much more memory efficient than the one returning an
        /// <see cref="IStreamGlobalOffset"/>. However, NO ARITHMETIC may be safely applied
        /// to the resulting value.</summary>
        /// <param name="ensureAtLeastOneAvailableByte"></param>
        /// <returns></returns>
        /// <exception cref="BugException"></exception>
        private uint _GetGlobalOffset(bool ensureAtLeastOneAvailableByte = false)
        {
            // Account for the flag parameter prior to computing global offset.
            if (ensureAtLeastOneAvailableByte && (0 >= RemainingBlockBytes)) {
                MoveToNextBlock();
            }
            ulong result = (_blockSize * _currentBlockNumber) + _currentBlockOffset;
            if (uint.MaxValue < result) {
                throw new BugException("Out of range global offset.");
            }
            return (uint)result;
        }

        internal void FillBuffer(IntPtr buffer, int bufferOffset, uint position,
            uint fillSize)
        {
            _pdb.FillBuffer(buffer, bufferOffset, position, fillSize);
            _currentBlockOffset += fillSize;
        }

        private int FindBlockIndex(uint globalOffset, out uint blockOffset)
        {
            for (int result = 0; result < _blocks.Length; result++) {
                uint blockNumber = _blocks[result];
                uint blockStartGlobalOffset = blockNumber * _blockSize;
                if (blockStartGlobalOffset > globalOffset) {
                    continue;
                }
                uint blockEndGlobalOffsetExcluded = blockStartGlobalOffset + _blockSize;
                if (blockEndGlobalOffsetExcluded <= globalOffset) {
                    continue;
                }
                // SetCurrentBlockIndex((uint)result);
                blockOffset = globalOffset - blockStartGlobalOffset;
                return result;
            }
            throw new BugException($"Unable to retrieve block matching global offset 0x{globalOffset:X8}.");
        }

        private void HandleEndOfBlock()
        {
            if (_currentBlockOffset == _blockSize) {
                MoveToNextBlockAllowEndOfStream();
                return;
            }
            if (_currentBlockOffset > _blockSize) {
                throw new BugException();
            }
        }

        //internal void HandlePadding(uint maxPaddingSize = byte.MaxValue)
        //{
        //    uint paddingSize;
        //    HandlePadding(maxPaddingSize, out paddingSize);
        //}

        /// <summary></summary>
        /// <param name="maxPaddingSize"></param>
        /// <param name="realPaddingSize"></param>
        /// <returns>The true padding bytes count.</returns>
        internal uint HandlePadding(uint maxPaddingSize)
        {
            if (_endOfStreamReached) {
                return 0;
            }
            // Initially we tought padding would align on a world boundary.
            // However it appears this doesn't always stand. So we can't
            // compute expected padding size ahead of time as we did until
            // those rare cases were discovered.
            byte firstCandidatePaddingByte = PeekByte();
            uint paddingBytesCount;
            switch (firstCandidatePaddingByte) {
                case 0xF3:
                    paddingBytesCount = 3;
                    break;
                case 0xF2:
                    paddingBytesCount = 2;
                    break;
                case 0xF1:
                    paddingBytesCount = 1;
                    break;
                default:
                    // No padding expected.
                    return 0;
            }
            IStreamGlobalOffset paddingStartGlobalOffset = GetGlobalOffset();
            uint result = Math.Min(paddingBytesCount, maxPaddingSize);
            uint remainingPadBytesCount = result;
            while (0 < remainingPadBytesCount) {
                byte paddingByte = ReadByte();
                if (paddingByte != (0xF0 + remainingPadBytesCount)) {
                    // Not a true padding.
                    SetGlobalOffset(paddingStartGlobalOffset, true);
                    return 0;
                }
                remainingPadBytesCount--;
            }
            return result;
        }

        private void MoveToNextBlock()
        {
            if (!MoveToNextBlockAllowEndOfStream()) {
                throw new BugException();
            }
        }
        
        private bool MoveToNextBlockAllowEndOfStream()
        {
            if (_endOfStreamReached) {
                _currentBlockIndex = 1 + _currentBlockIndex;
                return false;
            }
            if ((1 + _currentBlockIndex) >= _blocks.Length) {
                _endOfStreamReached = true;
                _currentBlockIndex += 1;
                return false;
            }
            SetPosition((uint)(_currentBlockIndex + 1), 0);
            return true;
        }

        internal byte PeekByte()
        {
            uint startOffset = Offset;
            try { return ReadByte(); }
            finally { this.Offset = startOffset; }
        }

        internal ushort PeekUInt16()
        {
            uint startOffset = Offset;
            try { return ReadUInt16(); }
            finally { this.Offset = startOffset; }
        }

        internal T Read<T>()
            where T : struct
        {
            AssertNotEndOfStream();
            uint remainingBlockBytes = RemainingBlockBytes;
            uint requiredBytes = (uint)Marshal.SizeOf(typeof(T));

            if (requiredBytes <= remainingBlockBytes) {
                // Fast read.
                T result = _pdb.Read<T>(_GetGlobalOffset());
                _currentBlockOffset += requiredBytes;
                // We may have consumed the whole block. In such a case we should set position
                // at beginning of the next one.
                if (0 == RemainingBlockBytes) {
                    MoveToNextBlock();
                }
                return result;
            }
            IntPtr buffer = IntPtr.Zero;
            try {
                if (int.MaxValue < requiredBytes) {
                    throw new NotSupportedException();
                }
                buffer = Marshal.AllocHGlobal((int)requiredBytes);
                int bufferOffset = 0;
                uint pendingReadSize = requiredBytes;
                while (0 < pendingReadSize) {
                    uint readSize = Math.Min(RemainingBlockBytes, pendingReadSize);
                    this.FillBuffer(buffer, bufferOffset, _GetGlobalOffset(), readSize);
                    pendingReadSize -= readSize;
                    bufferOffset += (int)readSize;
                    if (0 == RemainingBlockBytes) {
                        MoveToNextBlock();
                    }
                }
                return Marshal.PtrToStructure<T>(buffer);
            }
            finally {
                if (IntPtr.Zero != buffer) { Marshal.FreeHGlobal(buffer); }
            }
        }

        internal void Read(byte[] array)
        {
            if (null == array) {
                throw new ArgumentNullException(nameof(array));
            }
            AssertNotEndOfStream();
            uint requiredBytes = (uint)array.Length;
            uint arrayOffset = 0;
            while(0 < requiredBytes) {
                uint remainingBlockBytes = RemainingBlockBytes;
                uint readSize = Math.Min(requiredBytes, remainingBlockBytes);
                _pdb.Read(_GetGlobalOffset(), array, arrayOffset, readSize);
                _currentBlockOffset += readSize;
                requiredBytes -= readSize;
                arrayOffset += readSize;
                if (0 == requiredBytes) {
                    return;
                }
                MoveToNextBlock();
            }
            HandleEndOfBlock();
        }

        internal void ReadArray<T>(T[] into, ReadDelegate<T> reader)
        {
            if (null == into) {
                throw new ArgumentNullException(nameof(into));
            }
            AssertNotEndOfStream();
            ReadArray(into, 0, into.Length, reader);
        }

        internal void ReadArray<T>(T[] into, int startOffset, int length,
            ReadDelegate<T> reader)
        {
            if (null == into) {
                throw new ArgumentNullException(nameof(into));
            }
            if (0 > length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            AssertNotEndOfStream();
            if (0 == length) {
                // Nothing to do.
                return;
            }
            if ((0 > startOffset) || (startOffset >= into.Length)) {
                throw new ArgumentOutOfRangeException(nameof(startOffset));
            }
            int endOffset = startOffset + length - 1;
            if ((0 > endOffset) || (endOffset >= into.Length)) {
                throw new ArgumentOutOfRangeException(nameof(endOffset));
            }
            for (int index = 0; index < length; index++) {
                into[index] = reader();
            }
            HandleEndOfBlock();
        }

        internal byte ReadByte()
        {
            AssertNotEndOfStream();
            uint globalOffset = _GetGlobalOffset();
            byte result = _pdb.ReadByte(ref globalOffset);
            _currentBlockOffset += sizeof(byte);
            HandleEndOfBlock();
            return result;
        }

        internal string ReadNTBString(ref uint maxLength)
        {
            AssertNotEndOfStream();
            List<byte> bytes = new List<byte>();
            while (!_endOfStreamReached && (0 < maxLength)) {
                byte inputByte = ReadByte();
                maxLength--;
                if (0 == inputByte) {
                    // Sometimes, multiple NTBs may appear at end of string.
                    // We may have reached end of stream so we wouldn't be able to peek
                    // next byte.
                    if (!_endOfStreamReached && (0 != PeekByte())) {
                        break;
                    }
                }
                else {
                    // We don't want to add the NULL terminator to the string.
                    bytes.Add(inputByte);
                }
            }
            string result = Encoding.UTF8.GetString(bytes.ToArray());
            // It looks like some but not all NTB strings are further padded with additional
            // bytes to next 32 bits boundary. Padding bytes are 0xF3 0xF2 0xF1 (in that order).
            maxLength -= HandlePadding(maxLength);
            HandleEndOfBlock();
            return result;
        }

        internal object ReadVariant()
        {
            uint consumedBytes;
            return ReadVariant(out consumedBytes);
        }

        internal object ReadVariant(out uint consumedBytes)
        {
            ulong firstWord = ReadUInt16();
            if (0 == (0x8000 & firstWord)) {
                // Fast track.
                consumedBytes = sizeof(ushort);
                return firstWord;
            }
            // The first word is the value type.
            switch ((LEAF_ENUM_e)firstWord) {
                case LEAF_ENUM_e.Character:
                    consumedBytes = sizeof(ushort) + sizeof(byte);
                    return (ulong)ReadByte();
                case LEAF_ENUM_e.Integer:
                    consumedBytes = sizeof(ushort) + sizeof(uint);
                    return (ulong)ReadUInt32();
                case LEAF_ENUM_e.LongInteger:
                    consumedBytes = sizeof(ushort) + sizeof(ulong);
                    return (ulong)ReadUInt64();
                case LEAF_ENUM_e.Real128Bits:
                    consumedBytes = sizeof(ushort) + 16;
                    byte[] real128BitsResult = new byte[16];
                    ReadArray<byte>(real128BitsResult, ReadByte);
                    return real128BitsResult;
                case LEAF_ENUM_e.Short:
                    consumedBytes = sizeof(ushort) + sizeof(ushort);
                    return (ulong)ReadUInt16();
                case LEAF_ENUM_e.UnsignedInteger:
                    consumedBytes = sizeof(ushort) + sizeof(uint);
                    return (ulong)ReadUInt32();
                case LEAF_ENUM_e.UnsignedLongInteger:
                    consumedBytes = sizeof(ushort) + sizeof(ulong);
                    return (ulong)ReadUInt64();
                case LEAF_ENUM_e.UnsignedShort:
                    consumedBytes = sizeof(ushort) + sizeof(ushort);
                    return (ulong)ReadUInt16();
                //case LEAF_ENUM_e.LongInteger:
                //    return (long)ReadUInt64();
                //case LEAF_ENUM_e.UnsignedLongInteger:
                //    return (long)ReadUInt64();
                default:
                    if (Utils.IsValidBuiltinType((LEAF_ENUM_e)firstWord)) {
                        throw new NotSupportedException(
                            $"Unsupported builtin type identifier 0x{firstWord:X4}.");
                    }
                    throw new PDBFormatException(
                        $"Unrecognized builtin type identifier 0x{firstWord:X4}.");
            }
        }

        internal ushort ReadUInt16()
        {
            AssertNotEndOfStream();
            uint remainingBlockBytes = RemainingBlockBytes;
            ushort result;
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(ushort) <= remainingBlockBytes) {
                // Fast read.
                try { return _pdb.ReadUInt16(ref globalOffset); }
                finally {
                    _currentBlockOffset += sizeof(ushort);
                    HandleEndOfBlock();
                }
            }
            // Must cross block boundary.
            int unreadBytes = sizeof(ushort);
            result = 0;
            while (0 < remainingBlockBytes) {
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                // Not strictly required because we will switch to next block later.
                _currentBlockOffset += sizeof(byte);
                remainingBlockBytes--;
                unreadBytes--;
            }
            // End of block reached. 
            MoveToNextBlock();
            remainingBlockBytes = RemainingBlockBytes;
            while (0 < unreadBytes) {
                if (0 >= remainingBlockBytes) {
                    throw new BugException();
                }
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                // No need to decrement remainingBlockBytes because we are reading at
                // most three bytes which is guaranteed to be less than remaining block
                // bytes ...
                unreadBytes--;
                // ... however don't forget to adjust current block offset (BUG FIX)
                _currentBlockOffset += sizeof(byte);
            }
            HandleEndOfBlock();
            return result;
        }

        internal uint ReadUInt16AndCastToUInt32()
        {
            return (uint)ReadUInt16();
        }

        internal uint ReadUInt32()
        {
            AssertNotEndOfStream();
            uint remainingBlockBytes = RemainingBlockBytes;
            uint result;
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(uint) <= remainingBlockBytes) {
                // Fast read.
                try { return _pdb.ReadUInt32(ref globalOffset); }
                finally {
                    _currentBlockOffset += sizeof(uint);
                    HandleEndOfBlock();
                }
            }
            // Must cross block boundary.
            int unreadBytes = sizeof(uint);
            result = 0;
            while (0 < remainingBlockBytes) {
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                // Not strictly required because we will switch to next block later.
                _currentBlockOffset += sizeof(byte);
                remainingBlockBytes--;
                unreadBytes--;
            }
            // End of block reached. 
            MoveToNextBlock();
            remainingBlockBytes = RemainingBlockBytes;
            while (0 < unreadBytes) {
                if (0 >= remainingBlockBytes) {
                    throw new BugException();
                }
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                // No need to decrement remainingBlockBytes because we are reading at
                // most three bytes which is guaranteed to be less than remaining block
                // bytes ...
                unreadBytes--;
                // ... however don't forget to adjust current block offset (BUG FIX)
                _currentBlockOffset += sizeof(byte);
            }
            HandleEndOfBlock();
            return result;
        }

        internal ulong ReadUInt64()
        {
            AssertNotEndOfStream();
            uint remainingBlockBytes = RemainingBlockBytes;
            uint result;
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(ulong) <= remainingBlockBytes) {
                // Fast read.
                try { return _pdb.ReadUInt64(ref globalOffset); }
                finally {
                    _currentBlockOffset += sizeof(ulong);
                    HandleEndOfBlock();
                }
            }
            // Must cross block boundary.
            int unreadBytes = sizeof(ulong);
            result = 0;
            while (0 < remainingBlockBytes) {
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                // Not strictly required because we will switch to next block later.
                _currentBlockOffset += sizeof(byte);
                remainingBlockBytes--;
                unreadBytes--;
            }
            // End of block reached. 
            MoveToNextBlock();
            remainingBlockBytes = RemainingBlockBytes;
            while (0 < unreadBytes) {
                if (0 >= remainingBlockBytes) {
                    throw new BugException();
                }
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                // No need to decrement remainingBlockBytes because we are reading at
                // most three bytes which is guaranteed to be less than remaining block
                // bytes ...
                unreadBytes--;
                // ... however don't forget to adjust current block offset (BUG FIX)
                _currentBlockOffset += sizeof(byte);
            }
            HandleEndOfBlock();
            return result;
        }

        //private void SetCurrentBlockIndex(uint value, bool resetBlockOffset = false)
        //{
        //    if (value >= _blocks.Length) {
        //        throw new ArgumentOutOfRangeException(nameof(value));
        //    }
        //    _currentBlockIndex = (int)value;
        //    _currentBlockNumber = _blocks[value];
        //    if (resetBlockOffset) {
        //        _currentBlockOffset = 0;
        //    }
        //    _endOfStreamReached = false;
        //}

        internal void SetGlobalOffset(IStreamGlobalOffset value, bool doNotWarn = false)
        {
            if (!doNotWarn) {
                Console.WriteLine($"WARN : Setting reader global offset is not expected in normal course.");
            }
            uint newBlockOffset;
            int newBlockIndex = FindBlockIndex(value.Value, out newBlockOffset);
            if (0 > newBlockIndex) {
                throw new BugException();
            }
            SetPosition((uint)newBlockIndex, newBlockOffset);
            _endOfStreamReached = false;
        }

        private void SetPosition(uint newBlockIndex, uint newBlockOffset)
        {
            if (newBlockIndex >= _blocks.Length) {
                throw new ArgumentOutOfRangeException(nameof(newBlockIndex));
            }
            if (newBlockOffset >= _blockSize) {
                throw new ArgumentOutOfRangeException(nameof(newBlockOffset));
            }
            _currentBlockIndex = (int)newBlockIndex;
            _currentBlockNumber = _blocks[newBlockIndex];
            _currentBlockOffset = newBlockOffset;
            _endOfStreamReached = false;
        }

        private class GlobalOffset : IStreamGlobalOffset
        {
            private PdbStreamReader _owner;
            private int _blockIndex;
            private uint _blockOffset;

            public uint Value
            {
                get
                {
                    ulong candidateResult =
                        ((ulong)_owner._blockSize * _owner._blocks[_blockIndex]) +
                        _blockOffset;
                    if (uint.MaxValue < candidateResult) {
                        throw new BugException();
                    }
                    return (uint)candidateResult;
                }
            }

            private GlobalOffset(PdbStreamReader owner, int index, uint offset)
            {
                _owner = owner;
                _blockIndex = index;
                _blockOffset = offset;
            }

            internal GlobalOffset(PdbStreamReader owner, uint value)
            {
                _owner = owner;
                _blockIndex = owner.FindBlockIndex(value, out _blockOffset);
            }

            public IStreamGlobalOffset Add(uint relativeOffset)
            {
                uint initialGlobalOffsetValue = Value;
                uint currentBlockOffset;
                int currentBlockIndex = _owner.FindBlockIndex(initialGlobalOffsetValue,
                    out currentBlockOffset);
                uint remainingDisplacement = relativeOffset;
                while (true) {
                    uint candidateOffset = remainingDisplacement + currentBlockOffset;
                    if (candidateOffset < _owner._blockSize) {
                        return new GlobalOffset(this._owner, currentBlockIndex,
                            candidateOffset);
                    }
                    // Continue with next block.
                    uint availableBlockBytes = _owner._blockSize - currentBlockOffset;
                    remainingDisplacement -= availableBlockBytes;
                    if (++currentBlockIndex >= _owner._blocks.Length) {
                        throw new BugException(
                            $"Unable to add {relativeOffset} to global offset at {initialGlobalOffsetValue}.");
                    }
                    currentBlockOffset = 0;
                }
            }

            public int CompareTo(IStreamGlobalOffset? other)
            {
                if (null == other) {
                    throw new ArgumentNullException(nameof(other));
                }
                GlobalOffset? otherOffset = other as GlobalOffset;
                if (null == otherOffset) {
                    throw new NotSupportedException();
                }
                if (this._blockIndex > otherOffset._blockIndex) {
                    return 1;
                }
                if (this._blockIndex < otherOffset._blockIndex) {
                    return -1;
                }
                return this._blockOffset.CompareTo(otherOffset._blockOffset);
            }

            public IStreamGlobalOffset Subtract(uint relativeOffset)
            {
                uint initialGlobalOffsetValue = Value;
                uint currentBlockOffset;
                int currentBlockIndex = _owner.FindBlockIndex(initialGlobalOffsetValue,
                    out currentBlockOffset);
                uint remainingDisplacement = relativeOffset;
                while (true) {
                    if (currentBlockOffset > remainingDisplacement) {
                        _blockIndex = currentBlockIndex;
                        _blockOffset = currentBlockOffset - remainingDisplacement;
                        return this;
                    }
                    // Continue with previous block.
                    uint availableBlockBytes = currentBlockOffset + 1;
                    remainingDisplacement -= availableBlockBytes;
                    if (0 == currentBlockIndex--) {
                        throw new BugException(
                            $"Unable to subtract {relativeOffset} to global offset at {initialGlobalOffsetValue}.");
                    }
                    currentBlockOffset = _owner._blockSize - 1;
                }
            }
        }
    }
}
