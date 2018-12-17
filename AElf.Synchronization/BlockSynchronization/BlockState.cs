using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Synchronization.BlockSynchronization
{
    public class BlockState
    {
        private readonly IBlock _block;
        
        private Dictionary<byte[], int> _minersToConfirmations;

        public BlockBody BlockBody => _block?.Body; // todo refactor out
        public BlockHeader BlockHeader => _block?.Header; // todo refactor out

        public Hash Previous => _block.Header.PreviousBlockHash;
            
        public Hash BlockHash => _block.GetHash();
        public ulong Index => _block.Header.Index;

        public BlockState(IBlock block, BlockState previous)
        {
            _block = block.Clone();
            Init(previous);
        }

        private void Init(BlockState previous)
        {
            
        }
        
        public static bool operator ==(BlockState bs1, BlockState bs2)
        {
            return bs1?.Equals(bs2) ?? ReferenceEquals(bs2, null);
        }

        public static bool operator !=(BlockState bs1, BlockState bs2)
        {
            return !(bs1 == bs2);
        }
        
        public override bool Equals(Object obj)
        {
            var other = obj as BlockState;

            if (other == null)
                return false;

            // Instances are considered equal if the ReferenceId matches.
            return BlockHash == other.BlockHash;
        }

        public IBlock GetClonedBlock()
        {
            return _block.Clone();
        }
    }
}