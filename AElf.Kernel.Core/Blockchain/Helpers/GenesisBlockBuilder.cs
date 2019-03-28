﻿using System;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Blockchain.Helpers
{
    //TODO: Add GenesisBlockBuilder test case [Case]
    public class GenesisBlockBuilder
    {
        public Block Block { get; set; }

        public GenesisBlockBuilder Build(int chainId)
        {
            var block = new Block(Hash.Empty)
            {
                Header = new BlockHeader
                {
                    Height = KernelConstants.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty,
                    ChainId = chainId,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Empty
                },
                Body = new BlockBody()
            };

            // Genesis block is empty
            // TODO: Maybe add info like Consensus protocol in Genesis block

            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoots();
            block.Body.BlockHeader = block.Header.GetHash();
            Block = block;

            return this;
        }
    }
}