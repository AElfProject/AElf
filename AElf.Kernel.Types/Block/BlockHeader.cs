using AElf.Common;
using AElf.Kernel.Types;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockHeader: IBlockHeader
    {
        private Hash _blockHash;
        
        public BlockHeader(Hash preBlockHash)
        {
            PreviousBlockHash = preBlockHash;
        }

        public Hash GetHash()
        {
            if (_blockHash == null)
            {
                _blockHash = Hash.FromRawBytes(GetSignatureData());
            }

            return _blockHash;
        }

        public byte[] GetHashBytes()
        {
            if (_blockHash == null)
                _blockHash = Hash.FromRawBytes(GetSignatureData());

            return _blockHash.DumpByteArray();
        }

        private byte[] GetSignatureData()
        {
            var rawBlock = new BlockHeader
            {
                ChainId = ChainId,
                Height = Height,
                PreviousBlockHash = PreviousBlockHash?.Clone(),
                MerkleTreeRootOfTransactions = MerkleTreeRootOfTransactions?.Clone(),
                MerkleTreeRootOfWorldState = MerkleTreeRootOfWorldState?.Clone(),
                Bloom = Bloom,
                BlockExtraDatas = {BlockExtraDatas}
            };
            if (Height > ChainConsts.GenesisBlockHeight)
                rawBlock.Time = Time?.Clone();

            return rawBlock.ToByteArray();
        }
    }
}