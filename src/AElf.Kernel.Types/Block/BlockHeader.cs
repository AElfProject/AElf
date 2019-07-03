using System;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader
    {
        private Hash _blockHash;

        partial void OnConstruction()
        {
            Bloom = ByteString.CopyFrom(new Bloom().Data);
        }

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
            return GetHash().DumpByteArray();
        }

        private byte[] GetSignatureData()
        {
            CheckField();

            if (Signature.IsEmpty)
                return this.ToByteArray();

            var blockHeader = Clone();
            blockHeader.Signature = ByteString.Empty;
            return blockHeader.ToByteArray();
        }

        private void CheckField()
        {
            if (ChainId < 0)
            {
                throw new InvalidOperationException($"Invalid block header, ChainId is not correct: {this}");
            }

            if (Height < Constants.GenesisBlockHeight)
            {
                throw new InvalidOperationException($"Invalid block header, Height is not correct: {this}");
            }

            if (PreviousBlockHash == null)
            {
                throw new InvalidOperationException($"Invalid block header, PreviousBlockHash is not correct: {this}");
            }

            if (MerkleTreeRootOfTransactions == null || MerkleTreeRootOfWorldState == null ||
                MerkleTreeRootOfTransactionStatus == null)
            {
                throw new InvalidOperationException($"Invalid block header, MerkleTreeRoot is not correct: {this}");
            }

            if (Time == null)
            {
                throw new InvalidOperationException($"Invalid block header, Time is not correct: {this}");
            }

            if (Height > Constants.GenesisBlockHeight && SignerPubkey.IsEmpty)
            {
                throw new InvalidOperationException($"Invalid block header, SignerPubkey is not correct: {this}");
            }
        }
    }
}