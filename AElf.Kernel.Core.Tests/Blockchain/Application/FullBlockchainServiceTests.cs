using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Shouldly.ShouldlyExtensionMethods;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceTests : AElfKernelTestBase
    {
        private readonly FullBlockchainService _fullBlockchainService;
        private readonly ITransactionManager _transactionManager;

        public FullBlockchainServiceTests()
        {
            _fullBlockchainService = GetRequiredService<FullBlockchainService>();
            _transactionManager = GetRequiredService<ITransactionManager>();
        }

        [Fact]
        public async Task Create_Chain_Success()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };

            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldBeNull();
            var existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.ShouldBeNull();

            var createChainResult = await _fullBlockchainService.CreateChainAsync(block);

            chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldNotBeNull();
            chain.ShouldBe(createChainResult);

            existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.GetHash().ShouldBe(block.GetHash());
        }

        [Fact]
        public async Task Add_Block_Success()
        {
            var block = new Block
            {
                Height = 2,
                Header = new BlockHeader(),
                Body = new BlockBody()
            };
            for (var i = 0; i < 3; i++)
            {
                block.Body.AddTransaction(GenerateTransaction());
            }

            var existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.ShouldBeNull();

            await _fullBlockchainService.AddBlockAsync(block);
            
            existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.GetHash().ShouldBe(block.GetHash());
            existBlock.Body.TransactionsCount.ShouldBe(3);

            foreach (var tx in block.Body.TransactionList)
            {
                var existTransaction = await _transactionManager.GetTransaction(tx.GetHash());
                existTransaction.ShouldBe(tx);
            }
        }

        [Fact]
        public async Task Has_Block_ReturnTrue()
        {
            var mockChain = await MockNewChain();
            
            var result = await _fullBlockchainService.HasBlockAsync(mockChain.BestBranchBlocks[1].GetHash());
            result.ShouldBeTrue();
            
            result = await _fullBlockchainService.HasBlockAsync(mockChain.ForkBranchBlocks[1].GetHash());
            result.ShouldBeTrue();
            
            result = await _fullBlockchainService.HasBlockAsync(mockChain.AloneBlocks[1].GetHash());
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task Has_Block_ReturnFalse()
        {
            var result = await _fullBlockchainService.HasBlockAsync(Hash.FromString("Not Exist Block"));
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnNull()
        {
            var mockChain = await MockNewChain();
            
            var result = await _fullBlockchainService.GetBlockHashByHeightAsync(mockChain.Chain, 12);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnHash()
        {
            var mockChain = await MockNewChain();
            
            var result = await _fullBlockchainService.GetBlockHashByHeightAsync(mockChain.Chain, mockChain.BestBranchBlocks[8].Height);
            result.ShouldBe(mockChain.BestBranchBlocks[8].GetHash());
        }

        [Fact]
        public async Task Get_GetReservedBlockHashes_ReturnNull()
        {
            var result = await _fullBlockchainService.GetReversedBlockHashes(Hash.FromString("not exist"), 1);
            result.ShouldBeNull();
        }


        [Fact]
        public async Task Get_GetBlockHashes_ReturnNull()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var result = await _fullBlockchainService.GetBlockHashes(chain, Hash.FromString("not exist"), 1);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_GetReservedBlockHashes_ReturnHashes()
        {
            var mockChain = await MockNewChain();
            
            var result = await _fullBlockchainService.GetReversedBlockHashes(mockChain.BestBranchBlocks[2].GetHash(), 2);
            result.Count.ShouldBe(2);
            result[0].ShouldBe(mockChain.BestBranchBlocks[1].GetHash());
            result[1].ShouldBe(mockChain.BestBranchBlocks[0].GetHash());
        }

        [Fact]
        public async Task Get_Blocks_ReturnNull()
        {
            var result = await _fullBlockchainService.GetBlocksAsync(Hash.FromString("not exist"), 3);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_Blocks_ReturnBlocks()
        {
            var mockChain = await MockNewChain();
            
            var result = await _fullBlockchainService.GetBlocksAsync(mockChain.Chain.BestChainHash, 3);
            result.Count.ShouldBe(0);
            
            result = await _fullBlockchainService.GetBlocksAsync(mockChain.BestBranchBlocks[2].GetHash(), 3);
            result.Count.ShouldBe(3);
            result[0].GetHash().ShouldBe(mockChain.BestBranchBlocks[3].GetHash());
            result[1].GetHash().ShouldBe(mockChain.BestBranchBlocks[4].GetHash());
            result[2].GetHash().ShouldBe(mockChain.BestBranchBlocks[5].GetHash());
            
            result = await _fullBlockchainService.GetBlocksAsync(mockChain.BestBranchBlocks[7].GetHash(), 3);
            result.Count.ShouldBe(2);
            result[0].GetHash().ShouldBe(mockChain.BestBranchBlocks[8].GetHash());
            result[1].GetHash().ShouldBe(mockChain.BestBranchBlocks[9].GetHash());
        }

        [Fact]
        public async Task Get_GetBlockHashes_ReturnHashes()
        {
            var mockChain = await MockNewChain();

            var result =
                await _fullBlockchainService.GetBlockHashes(mockChain.Chain, mockChain.BestBranchBlocks[0].GetHash(),
                    2);
            result.Count.ShouldBe(2);
            result[0].ShouldBe(mockChain.BestBranchBlocks[1].GetHash()); //6c56
            result[1].ShouldBe(mockChain.BestBranchBlocks[2].GetHash()); //

            result = await _fullBlockchainService.GetBlockHashes(mockChain.Chain,
                mockChain.BestBranchBlocks[1].GetHash(), 1);
            result.Count.ShouldBe(1);
            result[0].ShouldBe(mockChain.BestBranchBlocks[2].GetHash()); //
        }

        [Fact]
        public async Task Get_Block_ByHeight_ReturnBlock()
        {
            var mockChain = await MockNewChain();
            
            var result = await _fullBlockchainService.GetBlockByHeightAsync(mockChain.BestBranchBlocks[3].Height);
            result.ShouldNotBeNull();
            result.GetHash().ShouldBe(mockChain.BestBranchBlocks[3].GetHash());
        }

        [Fact]
        public async Task Get_Block_ByHeight_ReturnNull()
        {
            var mockChain = await MockNewChain();
            
            var result = await _fullBlockchainService.GetBlockByHeightAsync(15);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_Block_ByHash_ReturnBlock()
        {
            var mockChain = await MockNewChain();
            
            var result = await _fullBlockchainService.GetBlockByHashAsync(mockChain.BestBranchBlocks[2].GetHash());
            result.ShouldNotBeNull();
            result.Height.ShouldBe(mockChain.BestBranchBlocks[2].Height);
        }

        [Fact]
        public async Task Get_Block_ByHash_ReturnNull()
        {
            var result = await _fullBlockchainService.GetBlockByHashAsync(Hash.FromString("Not Exist Block"));
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_Chain_ReturnChain()
        {
            var mockChain = await MockNewChain();
            
            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldNotBeNull();
        }

        [Fact]
        public async Task Get_Chain_ReturnNull()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldBeNull();
        }


        [Fact]
        public async Task Get_BestChain_ReturnBlockHeader()
        {
            var mockChain = await MockNewChain();
            
            var newBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = mockChain.Chain.BestChainHeight + 1,
                    PreviousBlockHash = Hash.FromString("New Branch")
                },
                Body = new BlockBody()
            };

            await _fullBlockchainService.AddBlockAsync(newBlock);
            var chain = await _fullBlockchainService.GetChainAsync();
            await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);

            var result = await _fullBlockchainService.GetBestChainLastBlock();
            result.Height.ShouldBe(mockChain.BestBranchBlocks.Last().Height);
            result.GetHash().ShouldBe(mockChain.BestBranchBlocks.Last().GetHash());
        }

        /// <summary>
        /// Mock a chain with a best branch, a fork branch and some alone blocks
        /// </summary>
        /// <returns>
        ///          Chain: best chain height 11
        ///         Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11
        ///    Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
        /// Longest Branch:                                         l -> m  -> n -> o -> p 
        ///    Fork Branch:                          q -> r -> s -> t -> u
        ///    Alone Block: v[3] w[8] x[13]
        /// </returns>
        private async Task<MockChain> MockNewChain()
        {
            var chain = await CreateNewChain();
            var bestBranchBlockList = new List<Block>();
            var longestBranchBlockList = new List<Block>();
            var forkBranchBlockList = new List<Block>();
            var aloneBlockList = new List<Block>();

            // Best branch
            for (var i = 0; i < 10; i++)
            {
                var newBlock = new Block
                {
                    Header = new BlockHeader
                    {
                        Height = chain.BestChainHeight + 1,
                        PreviousBlockHash = chain.BestChainHash
                    },
                    Body = new BlockBody()
                };
                bestBranchBlockList.Add(newBlock);

                await _fullBlockchainService.AddBlockAsync(newBlock);
                chain = await _fullBlockchainService.GetChainAsync();
                await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
                await _fullBlockchainService.SetBestChainAsync(chain, newBlock.Height, newBlock.GetHash());
            }
            
            // Longest branch
            var currentHeight = bestBranchBlockList[6].Height + 1;
            var previousBlockHash = bestBranchBlockList[6].GetHash();
            for (var i = 0; i < 5; i++)
            {
                var newBlock = new Block
                {
                    Header = new BlockHeader
                    {
                        Height = currentHeight,
                        PreviousBlockHash = previousBlockHash
                    },
                    Body = new BlockBody()
                };
                longestBranchBlockList.Add(newBlock);

                await _fullBlockchainService.AddBlockAsync(newBlock);
                chain = await _fullBlockchainService.GetChainAsync();
                await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
                
                currentHeight = currentHeight + 1;
                previousBlockHash = newBlock.GetHash();
            }
            
            // Fork branch
            currentHeight = bestBranchBlockList[3].Height + 1;
            previousBlockHash = bestBranchBlockList[3].GetHash();
            for (var i = 0; i < 5; i++)
            {
                var newBlock = new Block
                {
                    Header = new BlockHeader
                    {
                        Height = currentHeight,
                        PreviousBlockHash = previousBlockHash
                    },
                    Body = new BlockBody()
                };
                forkBranchBlockList.Add(newBlock);

                await _fullBlockchainService.AddBlockAsync(newBlock);
                chain = await _fullBlockchainService.GetChainAsync();
                await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
                
                currentHeight = currentHeight + 1;
                previousBlockHash = newBlock.GetHash();
            }
            
            // Alone blocks
            for (var i = 0; i < 3; i++)
            {
                var newBlock = new Block
                {
                    Header = new BlockHeader
                    {
                        Height = i * 5 + 3,
                        PreviousBlockHash = Hash.FromString(Guid.NewGuid().ToString())
                    },
                    Body = new BlockBody()
                };
                aloneBlockList.Add(newBlock);

                await _fullBlockchainService.AddBlockAsync(newBlock);
                chain = await _fullBlockchainService.GetChainAsync();
                await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
            }

            chain = await _fullBlockchainService.GetChainAsync();
            return new MockChain
            {
                Chain = chain,
                BestBranchBlocks = bestBranchBlockList,
                LongestBranchBlocks = longestBranchBlockList,
                ForkBranchBlocks = forkBranchBlockList,
                AloneBlocks = aloneBlockList
            };
        }
        
        private async Task<Chain> CreateNewChain()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            if (chain != null)
            {
                throw new InvalidOperationException();
            }

            var genesisBlock = new Block
            {
                Header = new BlockHeader
                {
                    Height = ChainConsts.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty
                },
                Body = new BlockBody()
            };
            chain = await _fullBlockchainService.CreateChainAsync(genesisBlock);
            return chain;
        }
        
        private Transaction GenerateTransaction()
        {
            var transaction = new Transaction
            {
                From = Address.Zero,
                To = Address.Zero,
                MethodName = Guid.NewGuid().ToString()
            };

            return transaction;
        }
    }

    public class MockChain
    {
        public Chain Chain { get; set; }
        
        public List<Block> BestBranchBlocks { get; set; }
        
        public List<Block> LongestBranchBlocks { get; set; }
        
        public List<Block> ForkBranchBlocks { get; set; }
        
        public List<Block> AloneBlocks { get; set; }
    }
}