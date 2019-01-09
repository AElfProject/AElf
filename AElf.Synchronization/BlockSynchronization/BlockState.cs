using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Synchronization.BlockSynchronization
{
    public class BlockState
    {
        private readonly IBlock _block;

        public BlockBody BlockBody => _block?.Body; // todo refactor out
        public BlockHeader BlockHeader => _block?.Header; // todo refactor out

        public Hash Previous => _block.Header.PreviousBlockHash;
            
        public Hash BlockHash => _block.GetHash();
        public ulong Index => _block.Header.Index;

        public BlockState PreviousState { get; private set; }

        public string Producer => _block?.Header?.P.ToByteArray().ToHex();

        public List<string> _miners;
        public List<string> _confirmations = new List<string>();
        
        public bool IsInCurrentBranch { get; set; }

        public BlockState(IBlock block, BlockState previous, bool isInCurrentBranch, List<string> miners)
        {
            _block = block.Clone();
            IsInCurrentBranch = isInCurrentBranch;
            _miners = miners;
            Init(previous);
        }

        private void Init(BlockState previous)
        {
            PreviousState = previous;
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

        public bool AddConfirmation(string pubKey)
        {
            if (_confirmations.Any(c => c.Equals(pubKey))) 
                return false;
            _confirmations.Add(pubKey);
            return _confirmations.Count >= Math.Ceiling(2d / 3d * _miners.Count);
        }

        public IBlock GetClonedBlock()
        {
            return _block.Clone();
        }

        public BlockState GetCopyBlockState()
        {
            return new BlockState(GetClonedBlock(), PreviousState, IsInCurrentBranch, _miners);
        }

        public override string ToString()
        {
            return _block?.ToString() ?? "null block";
        }

        public override int GetHashCode()
        {
            var hash = 1;
            if (BlockHash != null) hash ^= BlockHash.GetHashCode();
            if (Previous != null) hash ^= Previous.GetHashCode();
            if (Producer.IsNullOrEmpty()) hash ^= Producer.GetHashCode();
            if (Index != 0) hash ^= Index.GetHashCode();
            return hash;
        }
    }
}