using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel
{
    public class KernelTestHelper
    {
        public IBlockchainService BlockchainService { get; set; }

        public Chain Chain { get; set; }

        /// <summary>
        /// 12 Blocks: a -> b -> c -> d -> e -> f -> g -> h -> i -> j -> k
        /// </summary>
        public List<Block> BestBranchBlockList { get; set; }
        
        /// <summary>
        /// 5 Blocks: l -> m -> n -> o -> p 
        /// </summary>
        public List<Block> LongestBranchBlockList { get; set; }
        
        /// <summary>
        /// 5 Blocks: q -> r -> s -> t -> u
        /// </summary>
        public List<Block> ForkBranchBlockList { get; set; }
        
        /// <summary>
        /// 5 Blocks: v -> w -> x -> y -> z
        /// </summary>
        public List<Block> UnlinkedBranchBlockList { get; set; }

        public KernelTestHelper()
        {
            BestBranchBlockList = new List<Block>();
            LongestBranchBlockList = new List<Block>();
            ForkBranchBlockList = new List<Block>();
            UnlinkedBranchBlockList = new List<Block>();
        }

        /// <summary>
        /// Mock a chain with a best branch, and some fork branches
        /// </summary>
        /// <returns>
        ///       Mock Chain
        ///    BestChainHeight: 11
        /// LongestChainHeight: 13
        ///         LIB height: 5
        /// 
        ///             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
        ///        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
        ///     Longest Branch:                                   (h)-> l -> m  -> n  -> o  -> p 
        ///        Fork Branch:                    (e)-> q -> r -> s -> t -> u
        ///    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
        /// </returns>
        public async Task<Chain> MockChain()
        {
            Chain = await CreateChain();

            var genesisBlock = await BlockchainService.GetBlockByHashAsync(Chain.GenesisBlockHash);
            BestBranchBlockList.Add(genesisBlock);
            
            BestBranchBlockList.AddRange(await AddBestBranch(Chain));
            
            LongestBranchBlockList =
                await AddForkBranch(Chain, BestBranchBlockList[7].Height + 1, BestBranchBlockList[7].GetHash());
            
            ForkBranchBlockList =
                await AddForkBranch(Chain, BestBranchBlockList[4].Height + 1, BestBranchBlockList[4].GetHash());

            UnlinkedBranchBlockList =
                await AddForkBranch(Chain, 10, Hash.FromString("UnlinkBlock"));
            // Set lib
            Chain = await BlockchainService.GetChainAsync();
            await BlockchainService.SetIrreversibleBlockAsync(Chain, BestBranchBlockList[4].Height,
                BestBranchBlockList[4].GetHash());

            return Chain;
        }
        
        public Transaction GenerateEmptyTransaction()
        {
            var transaction = new Transaction
            {
                From = Address.Zero,
                To = Address.Zero,
                MethodName = Guid.NewGuid().ToString()
            };

            return transaction;
        }

        private async Task<Chain> CreateChain()
        {
            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty
                },
                Body = new BlockBody()
            };
            var chain = await BlockchainService.CreateChainAsync(genesisBlock);
            return chain;
        }
        
        private async Task<List<Block>> AddBestBranch(Chain chain)
        {
            var bestBranchBlockList = new List<Block>();

            for (var i = 0; i < 10; i++)
            {
                var newBlock = new Block
                {
                    Header = new BlockHeader
                    {
                        Height = chain.BestChainHeight + 1,
                        PreviousBlockHash = chain.BestChainHash,
                        Time = Timestamp.FromDateTime(DateTime.UtcNow)
                    },
                    Body = new BlockBody()
                };
                bestBranchBlockList.Add(newBlock);

                await BlockchainService.AddBlockAsync(newBlock);
                chain = await BlockchainService.GetChainAsync();
                await BlockchainService.AttachBlockToChainAsync(chain, newBlock);
                await BlockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());
            }

            return bestBranchBlockList;
        }
        
        private async Task<List<Block>> AddForkBranch(Chain chain, long startHeight, Hash startPreviousHash)
        {
            var forkBranchBlockList = new List<Block>();

            for (var i = 0; i < 5; i++)
            {
                var newBlock = new Block
                {
                    Header = new BlockHeader
                    {
                        Height = startHeight,
                        PreviousBlockHash = startPreviousHash,
                        Time = Timestamp.FromDateTime(DateTime.UtcNow)
                    },
                    Body = new BlockBody()
                };
                forkBranchBlockList.Add(newBlock);

                await BlockchainService.AddBlockAsync(newBlock);
                chain = await BlockchainService.GetChainAsync();
                await BlockchainService.AttachBlockToChainAsync(chain, newBlock);

                startHeight++;
                startPreviousHash = newBlock.GetHash();
            }

            return forkBranchBlockList;
        }
    }
}