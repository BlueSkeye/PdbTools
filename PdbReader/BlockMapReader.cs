
namespace PdbReader
{
    /// <summary>Provides a forward only reader of the blockmap content.</summary>
    internal class BlockMapReader
    {
        private readonly uint[] _blockMapBlocks;
        private uint _blockMapBlocksCount;
        /// <summary>Blocl size cpatured from the superblock at reader creation time/</summary>
        private readonly uint _blockSize;
        /// <summary>Index within <see cref="_blockMapBlocks"/> of the block map
        /// addresses being read.</summary>
        private uint _currentReaderBlockIndex;
        /// <summary>Index within <see cref="_pdb._mappedPdbView"/> of the first
        /// byte of <see cref="_blockMapBlocks[_currentReaderBlockIndex]"/></summary>
        private uint _currentReaderBlockStartOffset;
        private readonly Pdb _pdb;
        private uint _readerOffset;

        internal BlockMapReader(Pdb owner)
        {
            _pdb = owner ?? throw new ArgumentNullException(nameof(owner));
            MSFSuperBlock superBlock = _pdb.SuperBlock;
            _blockSize = superBlock.BlockSize;
            // Read list of blocks used for Stream Directory storage.
            uint blockMapOffset = owner.GetBlockOffset(superBlock.BlockMapAddr);
            if (_pdb.ShouldTraceStreamDirectory) {
                Console.WriteLine(
                    $"DBG : Block map addr {superBlock.BlockMapAddr}, offset {blockMapOffset}, block size {_blockSize}.");
            }
            uint blockMapEntryCount = Pdb.Ceil(superBlock.NumDirectoryBytes, superBlock.BlockSize);
            _blockMapBlocksCount = ComputeBlockMapBlocksCount(superBlock.BlockSize, blockMapEntryCount);
            if (_pdb.ShouldTraceStreamDirectory) {
                Console.Write($"DBG : {blockMapEntryCount} entries in {_blockMapBlocksCount} map blocks : ");
            }
            // We may occupy several adjacent blocks such as in System.pdb having
            // signature 29F46DCA159C4451ACD67C3F1B43470E2 where block size is 0x200
            // and blockMapEntryCount = 0x88
            _blockMapBlocks = new uint[blockMapEntryCount];
            uint offset = blockMapOffset;
            // Read block map blocks index.
            for (int index = 0; index < blockMapEntryCount; index++) {
                uint currentBlock = _pdb.ReadUInt32(ref offset);
                _blockMapBlocks[index] = currentBlock;
                _pdb.RegisterUsedBlock(currentBlock);
                if (_pdb.ShouldTraceStreamDirectory) {
                    if (0 < index) { Console.Write(", "); }
                    Console.Write(currentBlock);
                }
            }
            if (_pdb.ShouldTraceStreamDirectory) { Console.WriteLine(); }
            
            // Having gathered in _blockMapBlocks the list of block numbers making up the stream Directory
            // we are now ready to read the directory content.
            SetCurrentReaderBlock(0);
            return;
        }

        /// <summary>Returns a copy of the block map blocks/</summary>
        internal uint[] BlocksList => (uint[])_blockMapBlocks.Clone();

        /// <summary>This method is intended to be invoked after each read operation. Should we have reached
        /// the end of the block currently being read, it will ensure to initialize the reader to read from
        /// the next block next time a read operation will be invoked.</summary>
        /// <exception cref="BugException"></exception>
        private void AdjustReaderBlock()
        {
            if (_readerOffset < _currentReaderBlockStartOffset) {
                throw new BugException();
            }
            uint delta = _readerOffset - _currentReaderBlockStartOffset;
            if (delta >= _blockSize) {
                SetCurrentReaderBlock(++_currentReaderBlockIndex);
            }
        }

        /// <summary>Given block size (from super block) and the number of entries in the block map,
        /// compiute how many blocks are consumed by the block map.</summary>
        /// <param name="blockSize"></param>
        /// <param name="blockMapEntryCount"></param>
        /// <returns></returns>
        private static uint ComputeBlockMapBlocksCount(uint blockSize, uint blockMapEntryCount)
        {
            uint result = 1 + ((blockMapEntryCount - 1) / (8 * blockSize));
            return result;
        }

        internal uint ReadUInt32()
        {
            // TODO : Should check enough bytes remains in current block.
            uint result = _pdb.ReadUInt32(ref _readerOffset);
            AdjustReaderBlock();
            return result;
        }

        /// <summary>Configure the reader to be positioned on the first byte of the block having the given
        /// index within the <see cref="_blockMapBlocks"/> array.</summary>
        /// <param name="blockMapIndex"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void SetCurrentReaderBlock(uint blockMapIndex)
        {
            if (_blockMapBlocks.Length <= blockMapIndex) {
                throw new ArgumentOutOfRangeException(nameof(blockMapIndex));
            }
            uint moveToBlockNumber = _blockMapBlocks[blockMapIndex];
            _currentReaderBlockIndex = blockMapIndex;
            _currentReaderBlockStartOffset = _pdb.GetBlockOffset(moveToBlockNumber);
            _readerOffset = _currentReaderBlockStartOffset;
            if (_pdb.ShouldTraceStreamDirectory) {
                Console.WriteLine(
                    $"DBG : Moving to block map block {moveToBlockNumber} at offset {_currentReaderBlockStartOffset}.");
            }
        }
    }
}
