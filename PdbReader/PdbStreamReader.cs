using PdbReader.Microsoft.CodeView;
using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader
{
    /// <summary>Each instance of this class is bound to a constuctor initiated stream referenced by its id.
    /// The class allows for reading from this stream. Whenever an offset is used as a parameter, this offset
    /// is relative to the first byte of the stream - not an absolutre PDB file offset.</summary>
    internal class PdbStreamReader
    {
        internal delegate T ReadDelegate<T>();

        /// <summary>A stream is made up of blocks. This is the list of block indexes making up this stream.
        /// Order is significant. Initialized at construction time.</summary>
        private readonly uint[] _blocks;
        /// <summary>Block size as initialized from the PDB file super block at construction time.</summary>
        private readonly uint _blockSize;
        /// <summary>Index within <see cref="_blocks"/> of current block.
        /// WARNING : Never set this field value. Use CurrentBlockIndex setter instead.</summary>
        private int _currentBlockIndex;
        /// <summary>For optimization purpose, always equal to _blocks[_currentBlockIndex]</summary>
        private uint _currentBlockNumber;
        /// <summary>Index within current block of first unread byte.</summary>
        private uint _currentBlockOffset;
        private bool _endOfStreamReached = false;
        /// <summary>The <see cref="Pdb"/> instance the stream is bound to.</summary>
        private readonly Pdb _pdb;
        /// <summary>Total length of the underlying stream.</summary>
        private readonly uint _streamSize;

        internal PdbStreamReader(Pdb owner, uint streamIndex)
        {
            if (null == owner) { throw new ArgumentNullException(nameof(owner)); }
            _pdb = owner;
            _blocks = owner.GetStreamMap(streamIndex, out _streamSize);
            _blockSize = _pdb.SuperBlock.BlockSize;
            SetPosition(0, 0);
        }

        /// <summary>For debugging purpose. Retrieve the current absolute position in the underlying PDB
        /// file.</summary>
        internal uint AbsolutePdbFilePosition
        {
            get
            {
                uint currentBlockNumber = _blocks[_currentBlockIndex];
                uint result = (currentBlockNumber * _blockSize) + _currentBlockOffset;
                return result;
            }
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

        internal Pdb Owner => _pdb;

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

        internal PdbStreamReader EnsureAlignment(uint modulo)
        {
            uint delta = (this.Offset % modulo);
            if (0 < delta) {
                this.Offset += (modulo - delta);
            }
            return this;
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
        internal uint _GetGlobalOffset(bool ensureAtLeastOneAvailableByte = false)
        {
            // Account for the flag parameter prior to computing global offset.
            if (ensureAtLeastOneAvailableByte && (0 >= RemainingBlockBytes)) {
                MoveToNextBlockIgnoreNewOffset();
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

        private void MoveToNextBlock(out uint newGlobalOffset)
        {
            if (!MoveToNextBlockAllowEndOfStream()) {
                throw new BugException();
            }
            newGlobalOffset = GetGlobalOffset().Value;
        }

        private void MoveToNextBlockIgnoreNewOffset()
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
                    MoveToNextBlockIgnoreNewOffset();
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
                        MoveToNextBlockIgnoreNewOffset();
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
                MoveToNextBlockIgnoreNewOffset();
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

        internal void ReadArray<T>(T[] into, int startOffset, int length, ReadDelegate<T> reader)
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

        internal string ReadNTBString(bool allowExtraNTB = false)
        {
            uint maxLength = uint.MaxValue;
            return ReadNTBString(ref maxLength, allowExtraNTB);
        }

        internal string ReadNTBString(ref uint maxLength, bool allowExtraNTB = false)
        {
            AssertNotEndOfStream();
            List<byte> bytes = new List<byte>();
            while (!_endOfStreamReached && (0 < maxLength)) {
                byte inputByte = ReadByte();
                maxLength--;
                // We don't want to add the NULL terminator to the string.
                if (0 == inputByte) {
                    // Sometimes, multiple NTBs may appear at end of string.
                    // We may have reached end of stream so we wouldn't be able to peek
                    // next byte.
                    if (   !_endOfStreamReached
                        && (!allowExtraNTB || (0 != PeekByte())))
                    {
                        break;
                    }
                    continue;
                }
                bytes.Add(inputByte);
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
            switch ((LeafIndices)firstWord) {
                case LeafIndices.Character:
                    consumedBytes = sizeof(ushort) + sizeof(byte);
                    return (ulong)ReadByte();
                case LeafIndices.Integer:
                    consumedBytes = sizeof(ushort) + sizeof(uint);
                    return (ulong)ReadUInt32();
                case LeafIndices.LongInteger:
                    consumedBytes = sizeof(ushort) + sizeof(ulong);
                    return (ulong)ReadUInt64();
                case LeafIndices.Real128Bits:
                    consumedBytes = sizeof(ushort) + 16;
                    byte[] real128BitsResult = new byte[16];
                    ReadArray<byte>(real128BitsResult, ReadByte);
                    return real128BitsResult;
                case LeafIndices.Short:
                    consumedBytes = sizeof(ushort) + sizeof(ushort);
                    return (ulong)ReadUInt16();
                case LeafIndices.UnsignedInteger:
                    consumedBytes = sizeof(ushort) + sizeof(uint);
                    return (ulong)ReadUInt32();
                case LeafIndices.UnsignedLongInteger:
                    consumedBytes = sizeof(ushort) + sizeof(ulong);
                    return (ulong)ReadUInt64();
                case LeafIndices.UnsignedShort:
                    consumedBytes = sizeof(ushort) + sizeof(ushort);
                    return (ulong)ReadUInt16();
                //case LEAF_ENUM_e.LongInteger:
                //    return (long)ReadUInt64();
                //case LEAF_ENUM_e.UnsignedLongInteger:
                //    return (long)ReadUInt64();
                default:
                    if (CodeViewUtils.IsValidBuiltinType((LeafIndices)firstWord)) {
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
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(ushort) <= remainingBlockBytes) {
                // Fast read.
                try { return _pdb.ReadUInt16(ref globalOffset); }
                finally {
                    _currentBlockOffset += sizeof(ushort);
                    HandleEndOfBlock();
                }
            }
            return (ushort)_SlowRead(sizeof(ushort), remainingBlockBytes);
        }

        internal uint ReadUInt16AndCastToUInt32()
        {
            return (uint)ReadUInt16();
        }

        internal uint ReadUInt32()
        {
            AssertNotEndOfStream();
            uint remainingBlockBytes = RemainingBlockBytes;
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(uint) <= remainingBlockBytes) {
                // Fast read.
                try { return _pdb.ReadUInt32(ref globalOffset); }
                finally {
                    _currentBlockOffset += sizeof(uint);
                    HandleEndOfBlock();
                }
            }
            return (uint)_SlowRead(sizeof(uint), remainingBlockBytes);
        }

        internal ulong ReadUInt64()
        {
            AssertNotEndOfStream();
            uint remainingBlockBytes = RemainingBlockBytes;
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(ulong) <= remainingBlockBytes) {
                // Fast read.
                try { return _pdb.ReadUInt64(ref globalOffset); }
                finally {
                    _currentBlockOffset += sizeof(ulong);
                    HandleEndOfBlock();
                }
            }
            return (ulong)_SlowRead(sizeof(ulong), remainingBlockBytes);
        }

        /// <summary>From time to time, we need to skip some null bytes in the input stream.</summary>
        /// <returns></returns>
        internal PdbStreamReader SkipNullBytes()
        {
            // Some records have trailing NULL bytes before names. Skip them.
            while (0 == this.PeekByte()) { 
                this.ReadByte();
            }
            return this;
        }

        private ulong _SlowRead(int unreadBytes, uint remainingBlockBytes)
        {
            // Must cross block boundary.
            uint globalOffset = GetGlobalOffset().Value;
            ulong result = 0;
            int shiftCount = 0;
            while (0 < remainingBlockBytes) {
                // Note : globalOffset is incremented by the reader.
                result += (ulong)(_pdb.ReadByte(ref globalOffset) << shiftCount);
                // Not strictly required because we will switch to next block later.
                _currentBlockOffset += sizeof(byte);
                remainingBlockBytes--;
                unreadBytes--;
                shiftCount += 8;
            }
            // End of block reached. 
            MoveToNextBlock(out globalOffset);
            remainingBlockBytes = RemainingBlockBytes;
            while (0 < unreadBytes) {
                if (0 >= remainingBlockBytes) {
                    throw new BugException();
                }
                // Note : globalOffset is incremented by the reader.
                result += (ulong)(_pdb.ReadByte(ref globalOffset) << shiftCount);
                // No need to decrement remainingBlockBytes because we are reading at
                // most three bytes which is guaranteed to be less than remaining block
                // bytes ...
                unreadBytes--;
                // ... however don't forget to adjust current block offset (BUG FIX)
                _currentBlockOffset += sizeof(byte);
                shiftCount += 8;
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
