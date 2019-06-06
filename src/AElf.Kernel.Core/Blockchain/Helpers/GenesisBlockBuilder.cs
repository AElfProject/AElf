using System;
using AElf.Common;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Blockchain.Helpers
{
    // TODO: It seems we never use this except test projects.
    public class GenesisBlockBuilder
    {
        public Block Block { get; set; }

        public GenesisBlockBuilder Build(int chainId)
        {
            var block = new Block(Hash.Empty)
            {
                Header = new BlockHeader
                {
                    Height = Constants.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty,
                    ChainId = chainId,
                    Time = TimestampHelper.GetUtcNow(),
                    MerkleTreeRootOfWorldState = Hash.Empty
                },
                Body = new BlockBody()
            };

            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoot();
            block.Body.BlockHeader = block.Header.GetHash();         
            Block = block;

            return this;
        }
    }
}