using System;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Blockchain.Helpers
{
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
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    MerkleTreeRootOfWorldState = Hash.Empty
                },
                Body = new BlockBody()
            };

            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoots();
            block.Body.BlockHeader = block.Header.GetHash();         
            Block = block;

            return this;
        }
    }
}