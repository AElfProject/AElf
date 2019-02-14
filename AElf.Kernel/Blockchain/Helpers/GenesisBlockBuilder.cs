using System;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Blockchain.Helpers
{
    public class GenesisBlockBuilder
    {
        public Block Block { get; set; }

        public GenesisBlockBuilder Build(int chainId)
        {
            var block = new Block(Hash.Genesis)
            {
                Header = new BlockHeader
                {
                    Height = GlobalConfig.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Genesis,
                    ChainId = chainId,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Default
                },
                Body = new BlockBody()
            };

            // Genesis block is empty
            // TODO: Maybe add info like Consensus protocol in Genesis block

            block.Complete(DateTime.UtcNow);          
            Block = block;

            return this;
        }
    }
}