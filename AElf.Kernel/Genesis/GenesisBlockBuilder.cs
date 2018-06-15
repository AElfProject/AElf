using System;
using System.Collections.Generic;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Services;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel
{
    public class GenesisBlockBuilder
    {
        public Block Block { get; set; }

        public GenesisBlockBuilder Build(Hash chainId)
        {
            var block = new Block(Hash.Default)
            {
                Header = new BlockHeader
                {
                    Index = 0,
                    PreviousBlockHash = Hash.Default,
                    ChainId = chainId,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };

            // Genesis block is empty
            // TODO: Maybe add info like Consensus protocol in Genesis block

            block.FillTxsMerkleTreeRootInHeader();
            
            Block = block;

            return this;
        }
    }
}