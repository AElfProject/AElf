using AElf.Common;
using AElf.Kernel.Types;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockHeader: IBlockHeader
    {
        partial void OnConstruction()
        {
            Bloom = ByteString.CopyFrom(new Bloom().Data);
        }

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

        public Hash GetHashWithoutCache()
        {
            _blockHash = null;
            return GetHash();
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
            // TODO: Remove this judgement.
            if (Height > KernelConstants.GenesisBlockHeight)
                rawBlock.Time = Time?.Clone();

            return rawBlock.ToByteArray();
        }
    }
}