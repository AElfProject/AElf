using System;
using AElf.Kernel.Types;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;

namespace AElf.Kernel
{
    public class GenesisBlockBuilder
    {
        public Block Block { get; set; }

        public GenesisBlockBuilder Build(Hash chainId)
        {
            var block = new Block(Hash.Genesis)
            {
                Header = new BlockHeader
                {
                    Index = 0,
                    PreviousBlockHash = Hash.Genesis,
                    ChainId = chainId,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Default
                },
                Body = new BlockBody()
            };

            // Genesis block is empty
            // TODO: Maybe add info like Consensus protocol in Genesis block

            block.Complete();          
            Block = block;

            return this;
        }
    }
}