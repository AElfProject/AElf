using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Infrastructure;
using AElf.Types;
using Shouldly;
using Shouldly.ShouldlyExtensionMethods;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceTests : AElfKernelWithChainTestBase
    {
        private readonly FullBlockchainService _fullBlockchainService;
        private readonly ITransactionManager _transactionManager;
        private readonly IChainManager _chainManager;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ILocalEventBus _eventBus;

        public FullBlockchainServiceTests()
        {
            _fullBlockchainService = GetRequiredService<FullBlockchainService>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _chainManager = GetRequiredService<IChainManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task Add_Block_Success()
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < 3; i++)
            {
                var transaction = _kernelTestHelper.GenerateTransaction();
                transactions.Add(transaction);
            }

            var block = _kernelTestHelper.GenerateBlock(0, Hash.Empty, transactions);

            var existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.ShouldBeNull();

            await _fullBlockchainService.AddBlockAsync(block);

            existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.ShouldBe(block);

            var blockHeader = await _fullBlockchainService.GetBlockHeaderByHashAsync(block.GetHash());
            blockHeader.ShouldBe(block.Header);
        }

        [Fact]
        public async Task Has_Block_ReturnTrue()
        {
            var result = await _fullBlockchainService.HasBlockAsync(_kernelTestHelper.BestBranchBlockList[1].GetHash());
            result.ShouldBeTrue();

            result = await _fullBlockchainService.HasBlockAsync(_kernelTestHelper.LongestBranchBlockList[1].GetHash());
            result.ShouldBeTrue();

            result = await _fullBlockchainService.HasBlockAsync(_kernelTestHelper.ForkBranchBlockList[1].GetHash());
            result.ShouldBeTrue();

            result = await _fullBlockchainService.HasBlockAsync(_kernelTestHelper.NotLinkedBlockList[1].GetHash());
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task Has_Block_ReturnFalse()
        {
            var result = await _fullBlockchainService.HasBlockAsync(HashHelper.ComputeFrom("Not Exist Block"));
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnNull()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            var result =
                await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 14, chain.BestChainHash);
            result.ShouldBeNull();

            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 20, chain.LongestChainHash);
            result.ShouldBeNull();

            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 14,
                _kernelTestHelper.ForkBranchBlockList.Last().GetHash());
            result.ShouldBeNull();

            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 15,
                _kernelTestHelper.NotLinkedBlockList.Last().GetHash());
            result.ShouldBeNull();
            
            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 8,
                _kernelTestHelper.NotLinkedBlockList.Last().GetHash());
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_BlockHash_ByHeight_ReturnHash()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            var result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain,
                _kernelTestHelper.BestBranchBlockList[8].Height, chain.BestChainHash);
            result.ShouldBe(_kernelTestHelper.BestBranchBlockList[8].GetHash());

            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain,
                _kernelTestHelper.LongestBranchBlockList[3].Height, chain.LongestChainHash);
            result.ShouldBe(_kernelTestHelper.LongestBranchBlockList[3].GetHash());

            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain,
                _kernelTestHelper.ForkBranchBlockList[3].Height,
                _kernelTestHelper.ForkBranchBlockList.Last().GetHash());
            result.ShouldBe(_kernelTestHelper.ForkBranchBlockList[3].GetHash());

            // search irreversible section of the chain
            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain,
                chain.LastIrreversibleBlockHeight - 1, chain.BestChainHash);
            result.ShouldBe(_kernelTestHelper.BestBranchBlockList[3].GetHash());
        }

        [Fact]
        public async Task Set_BestChain_Success()
        {
            BestChainFoundEventData eventData = null;
            _eventBus.Subscribe<BestChainFoundEventData>(d =>
            {
                eventData = d;
                return Task.CompletedTask;
            });
            
            var chain = await _fullBlockchainService.GetChainAsync();

            chain.BestChainHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList.Last().Height);
            chain.BestChainHash.ShouldBe(_kernelTestHelper.BestBranchBlockList.Last().GetHash());

            await _fullBlockchainService.SetBestChainAsync(chain, chain.LongestChainHeight, chain.LongestChainHash);

            chain = await _fullBlockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(_kernelTestHelper.LongestBranchBlockList.Last().Height);
            chain.BestChainHash.ShouldBe(_kernelTestHelper.LongestBranchBlockList.Last().GetHash());
            
            eventData.ShouldNotBeNull();
            eventData.BlockHash.ShouldBe(chain.BestChainHash);
            eventData.BlockHeight.ShouldBe(chain.BestChainHeight);
        }

        [Fact]
        public async Task Get_GetReservedBlockHashes_ReturnNull()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            var result = await _fullBlockchainService.GetReversedBlockIndexes(HashHelper.ComputeFrom("not exist"), 1);
            result.Count.ShouldBe(0);

            result = await _fullBlockchainService.GetReversedBlockIndexes(chain.GenesisBlockHash, 1);
            result.Count.ShouldBe(0);
        }

        [Fact]
        public async Task Get_ReversedBlockHashes_ReturnEmpty()
        {
            var result =
                await _fullBlockchainService.GetReversedBlockIndexes(_kernelTestHelper.BestBranchBlockList[2].GetHash(),
                    0);
            result.Count.ShouldBe(0);
        }

        [Fact]
        public async Task Get_GetReservedBlockHashes_ReturnHashes()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            var result =
                await _fullBlockchainService.GetReversedBlockIndexes(_kernelTestHelper.BestBranchBlockList[5].GetHash(),
                    3);
            result.Count.ShouldBe(3);
            result[0].BlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[4].GetHash());
            result[1].BlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[3].GetHash());
            result[2].BlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[2].GetHash());

            result = await _fullBlockchainService.GetReversedBlockIndexes(
                _kernelTestHelper.BestBranchBlockList[3].GetHash(), 4);
            result.Count.ShouldBe(3);
            result[0].BlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[2].GetHash());
            result[1].BlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[1].GetHash());
            result[2].BlockHash.ShouldBe(chain.GenesisBlockHash);
        }

        [Fact]
        public async Task Get_Blocks_ReturnEmpty()
        {
            var result = await _fullBlockchainService.GetBlocksInBestChainBranchAsync(HashHelper.ComputeFrom("not exist"), 3);
            result.Count.ShouldBe(0);
        }

        [Fact]
        public async Task Get_Blocks_ReturnBlocks()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            var result = await _fullBlockchainService.GetBlocksInBestChainBranchAsync(chain.BestChainHash, 3);
            result.Count.ShouldBe(0);

            result = await _fullBlockchainService.GetBlocksInBestChainBranchAsync(
                _kernelTestHelper.BestBranchBlockList[3].GetHash(), 3);
            result.Count.ShouldBe(3);
            result[0].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[4].GetHash());
            result[1].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[5].GetHash());
            result[2].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[6].GetHash());

            result = await _fullBlockchainService.GetBlocksInBestChainBranchAsync(
                _kernelTestHelper.BestBranchBlockList[8].GetHash(), 3);
            result.Count.ShouldBe(2);
            result[0].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[9].GetHash());
            result[1].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[10].GetHash());

            _fullBlockchainService.GetBlocksInBestChainBranchAsync(
                    _kernelTestHelper.LongestBranchBlockList[0].GetHash(), 3)
                .ShouldThrow("wrong branch", typeof(Exception));
        }

        [Fact]
        public async Task Get_GetBlockHashes_ReturnEmpty()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var notExistHash = HashHelper.ComputeFrom("not exist");

            var result = await _fullBlockchainService.GetBlockHashesAsync(chain, notExistHash, 1, chain.BestChainHash);
            result.Count.ShouldBe(0);

            result = await _fullBlockchainService.GetBlockHashesAsync(chain, notExistHash, 1, chain.LongestChainHash);
            result.Count.ShouldBe(0);

            result = await _fullBlockchainService.GetBlockHashesAsync(chain, notExistHash, 1,
                _kernelTestHelper.ForkBranchBlockList.Last().GetHash());
            result.Count.ShouldBe(0);

            await _fullBlockchainService.GetBlockHashesAsync(chain, chain.BestChainHash, 10, chain.BestChainHash)
                .ContinueWith(p => p.Result.Count.ShouldBe(0));

            await _fullBlockchainService.GetBlockHashesAsync(chain, chain.LongestChainHash, 10, chain.LongestChainHash)
                .ContinueWith(p => p.Result.Count.ShouldBe(0));

            await _fullBlockchainService.GetBlockHashesAsync(chain,
                    _kernelTestHelper.ForkBranchBlockList.Last().GetHash(), 10,
                    _kernelTestHelper.ForkBranchBlockList.Last().GetHash())
                .ContinueWith(p => p.Result.Count.ShouldBe(0));
        }

        [Fact]
        public async Task Get_GetBlockHashes_ThrowInvalidOperationException()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            await _fullBlockchainService
                .GetBlockHashesAsync(chain, _kernelTestHelper.BestBranchBlockList[5].GetHash(), 2,
                    _kernelTestHelper.NotLinkedBlockList.Last().GetHash())
                .ShouldThrowAsync<InvalidOperationException>();
        }
        
        [Fact]
        public async Task GetBlockHashes_WrongBranch_ThrowException()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            await _fullBlockchainService
                .GetBlockHashesAsync(chain, _kernelTestHelper.LongestBranchBlockList[0].GetHash(), 2,
                    _kernelTestHelper.BestBranchBlockList.Last().GetHash())
                .ShouldThrowAsync<Exception>();
        }

        [Fact]
        public async Task Get_GetBlockHashes_ReturnHashes()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            var result = await _fullBlockchainService.GetBlockHashesAsync(chain,
                _kernelTestHelper.BestBranchBlockList[1].GetHash(), 3,
                _kernelTestHelper.BestBranchBlockList[6].GetHash());
            result.Count.ShouldBe(3);
            result[0].ShouldBe(_kernelTestHelper.BestBranchBlockList[2].GetHash());
            result[1].ShouldBe(_kernelTestHelper.BestBranchBlockList[3].GetHash());
            result[2].ShouldBe(_kernelTestHelper.BestBranchBlockList[4].GetHash());

            result = await _fullBlockchainService.GetBlockHashesAsync(chain,
                _kernelTestHelper.BestBranchBlockList[7].GetHash(), 10,
                _kernelTestHelper.BestBranchBlockList[9].GetHash());
            result.Count.ShouldBe(2);
            result[0].ShouldBe(_kernelTestHelper.BestBranchBlockList[8].GetHash());
            result[1].ShouldBe(_kernelTestHelper.BestBranchBlockList[9].GetHash());

            result = await _fullBlockchainService.GetBlockHashesAsync(chain,
                _kernelTestHelper.LongestBranchBlockList[0].GetHash(), 2,
                _kernelTestHelper.LongestBranchBlockList[3].GetHash());
            result.Count.ShouldBe(2);
            result[0].ShouldBe(_kernelTestHelper.LongestBranchBlockList[1].GetHash());
            result[1].ShouldBe(_kernelTestHelper.LongestBranchBlockList[2].GetHash());

            result = await _fullBlockchainService.GetBlockHashesAsync(chain,
                _kernelTestHelper.LongestBranchBlockList[0].GetHash(), 10,
                _kernelTestHelper.LongestBranchBlockList[3].GetHash());
            result.Count.ShouldBe(3);
            result[0].ShouldBe(_kernelTestHelper.LongestBranchBlockList[1].GetHash());
            result[1].ShouldBe(_kernelTestHelper.LongestBranchBlockList[2].GetHash());
            result[2].ShouldBe(_kernelTestHelper.LongestBranchBlockList[3].GetHash());
        }

        [Fact]
        public async Task Get_Block_ByHeight_ReturnBlock()
        {
            var result =
                await _fullBlockchainService.GetBlockByHeightInBestChainBranchAsync(
                    _kernelTestHelper.BestBranchBlockList[3].Height);
            result.ShouldNotBeNull();
            result.GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[3].GetHash());
        }

        [Fact]
        public async Task Get_Block_ByHeight_ReturnNull()
        {
            var result = await _fullBlockchainService.GetBlockByHeightInBestChainBranchAsync(15);
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_Block_ByHash_ReturnBlock()
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < 3; i++)
            {
                transactions.Add(_kernelTestHelper.GenerateTransaction());
            }

            var block = _kernelTestHelper.GenerateBlock(0, Hash.Empty, transactions);

            await _fullBlockchainService.AddBlockAsync(block);
            var result = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            result.GetHash().ShouldBe(block.GetHash());
            result.Body.TransactionIds[0].ShouldBe(block.Body.TransactionIds[0]);
            result.Body.TransactionIds[1].ShouldBe(block.Body.TransactionIds[1]);
            result.Body.TransactionIds[2].ShouldBe(block.Body.TransactionIds[2]);
        }

        [Fact]
        public async Task Get_Block_ByHash_ReturnNull()
        {
            var result = await _fullBlockchainService.GetBlockByHashAsync(HashHelper.ComputeFrom("Not Exist Block"));
            result.ShouldBeNull();
        }

        [Fact]
        public async Task Get_BlockHeaderByHash_ReturnHeader()
        {
            var blockHeader =
                await _fullBlockchainService.GetBlockHeaderByHeightAsync(
                    _kernelTestHelper.BestBranchBlockList[2].Height);
            blockHeader.GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[2].GetHash());
        }

        [Fact]
        public async Task Get_Chain_ReturnChain()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldNotBeNull();
        }

        [Fact]
        public async Task Get_BestChain_ReturnBlockHeader()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            var newBlock = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, HashHelper.ComputeFrom("New Branch"));

            await _fullBlockchainService.AddBlockAsync(newBlock);
            chain = await _fullBlockchainService.GetChainAsync();
            await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);

            var result = await _fullBlockchainService.GetBestChainLastBlockHeaderAsync();
            result.Height.ShouldBe(_kernelTestHelper.BestBranchBlockList.Last().Height);
            result.GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList.Last().GetHash());
        }

        [Fact]
        public async Task Get_BlocksInBestChainBranch_ReturnHashes()
        {
            var result =
                await _fullBlockchainService.GetBlocksInBestChainBranchAsync(
                    _kernelTestHelper.BestBranchBlockList[0].GetHash(),
                    2);
            result.Count.ShouldBe(2);
            result[0].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[1].GetHash());
            result[1].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[2].GetHash());

            result = await _fullBlockchainService.GetBlocksInBestChainBranchAsync(
                _kernelTestHelper.BestBranchBlockList[7].GetHash(), 10);
            result.Count.ShouldBe(3);
            result[0].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[8].GetHash());
            result[1].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[9].GetHash());
            result[2].GetHash().ShouldBe(_kernelTestHelper.BestBranchBlockList[10].GetHash());
        }

        [Fact]
        public async Task Get_BlocksInLongestChainBranchAsync_ReturnHashes()
        {
            var result =
                await _fullBlockchainService.GetBlocksInLongestChainBranchAsync(
                    _kernelTestHelper.LongestBranchBlockList[0].GetHash(), 2);
            result.Count.ShouldBe(2);
            result[0].GetHash().ShouldBe(_kernelTestHelper.LongestBranchBlockList[1].GetHash());
            result[1].GetHash().ShouldBe(_kernelTestHelper.LongestBranchBlockList[2].GetHash());

            result = await _fullBlockchainService.GetBlocksInLongestChainBranchAsync(
                _kernelTestHelper.LongestBranchBlockList[0].GetHash(), 20);
            int count = 10;
            result.Count.ShouldBe(count);
            for (int i = 0; i < count; i++)
            {
                result[i].GetHash().ShouldBe(_kernelTestHelper.LongestBranchBlockList[i + 1].GetHash());
            }
        }

        [Fact]
        public async Task Set_IrreversibleBlock_Test()
        {
            NewIrreversibleBlockFoundEvent eventData = null;
            _eventBus.Subscribe<NewIrreversibleBlockFoundEvent>(d =>
            {
                eventData = d;
                return Task.CompletedTask;
            });
            
            var chain = await _fullBlockchainService.GetChainAsync();
            {
                //         LIB height: 7
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch:                                   (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch:                    (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[6]
                    .Height, _kernelTestHelper.BestBranchBlockList[6].GetHash());

                chain = await _fullBlockchainService.GetChainAsync();
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[6].GetHash());
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[6].Height);

                eventData.ShouldNotBeNull();
                eventData.BlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[6].GetHash());
                eventData.BlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[6].Height);
                eventData.PreviousIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[4].GetHash());
                eventData.PreviousIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[4].Height);
                    
                var blockLink =
                    await _chainManager.GetChainBlockLinkAsync(_kernelTestHelper.BestBranchBlockList[6].GetHash());
                while (blockLink != null)
                {
                    blockLink.IsIrreversibleBlock.ShouldBeTrue();
                    var chainBlockIndex = await _chainManager.GetChainBlockIndexAsync(blockLink.Height);
                    chainBlockIndex.BlockHash.ShouldBe(blockLink.BlockHash);

                    blockLink = await _chainManager.GetChainBlockLinkAsync(blockLink.PreviousBlockHash);
                }
            }

            {
                //         LIB height: 11
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch: (-)                               (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch: (-)                (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                             v(-) -> w  -> x  -> y  -> z
                eventData = null;
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[10]
                    .Height, _kernelTestHelper.BestBranchBlockList[10].GetHash());

                chain = await _fullBlockchainService.GetChainAsync();
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].GetHash());
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].Height);

                eventData.ShouldNotBeNull();
                eventData.BlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].GetHash());
                eventData.BlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].Height);
                eventData.PreviousIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[6].GetHash());
                eventData.PreviousIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[6].Height);
                
                var blockLink =
                    await _chainManager.GetChainBlockLinkAsync(_kernelTestHelper.BestBranchBlockList[10].GetHash());
                while (blockLink != null)
                {
                    blockLink.IsIrreversibleBlock.ShouldBeTrue();
                    var chainBlockIndex = await _chainManager.GetChainBlockIndexAsync(blockLink.Height);
                    chainBlockIndex.BlockHash.ShouldBe(blockLink.BlockHash);

                    blockLink = await _chainManager.GetChainBlockLinkAsync(blockLink.PreviousBlockHash);
                }
            }

            {
                // Set lib failed
                eventData = null;
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[9]
                    .Height, _kernelTestHelper.BestBranchBlockList[9].GetHash());
                
                eventData.ShouldBeNull();
            }
        }

        [Fact]
        public async Task Attach_New_Block_To_Chain_Test()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var newBlock = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash,
                new List<Transaction> {_kernelTestHelper.GenerateTransaction()});
            await _fullBlockchainService.AddBlockAsync(newBlock);
            var status = await _fullBlockchainService.AttachBlockToChainAsync(chain, newBlock);
            status.ShouldBe(BlockAttachOperationStatus.NewBlockLinked);
        }
        
        [Fact]
        public async Task Attach_Exist_Block_To_Chain_Test()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var block = _kernelTestHelper.BestBranchBlockList.Last();
            await _fullBlockchainService.AddBlockAsync(block);
            var status = await _fullBlockchainService.AttachBlockToChainAsync(chain, block);
            status.ShouldBe(BlockAttachOperationStatus.NewBlockLinked);
            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
            chainBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionSuccess);

            chain = await _fullBlockchainService.GetChainAsync();
            block = _kernelTestHelper.LongestBranchBlockList.Last();
            await _fullBlockchainService.AddBlockAsync(block);
            status = await _fullBlockchainService.AttachBlockToChainAsync(chain, block);
            status.ShouldBe(BlockAttachOperationStatus.NewBlockLinked);
            chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
            chainBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionNone);
        }

        [Fact]
        public async Task Get_DiscardedBranch_Test()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            {
                //         LIB height: 5
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch:                                   (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch:                    (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
                var discardedBranch = await _fullBlockchainService.GetDiscardedBranchAsync(chain);

                discardedBranch.BranchKeys.Count.ShouldBe(0);
                discardedBranch.NotLinkedKeys.Count.ShouldBe(0);
            }

            {
                //         LIB height: 7
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch:                                   (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch:                    (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[6]
                    .Height, _kernelTestHelper.BestBranchBlockList[6].GetHash());
                chain = await _fullBlockchainService.GetChainAsync();

                var discardedBranch = await _fullBlockchainService.GetDiscardedBranchAsync(chain);

                discardedBranch.BranchKeys.Count.ShouldBe(1);
                discardedBranch.BranchKeys[0]
                    .ShouldBe(_kernelTestHelper.ForkBranchBlockList.Last().GetHash().ToStorageKey());

                discardedBranch.NotLinkedKeys.Count.ShouldBe(0);
            }

            {
                //         LIB height: 9
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch:                                   (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch: (-)                (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[8]
                    .Height, _kernelTestHelper.BestBranchBlockList[8].GetHash());
                chain = await _fullBlockchainService.GetChainAsync();

                var discardedBranch = await _fullBlockchainService.GetDiscardedBranchAsync(chain);

                discardedBranch.BranchKeys.Count.ShouldBe(2);
                discardedBranch.BranchKeys
                    .ShouldContain(_kernelTestHelper.ForkBranchBlockList.Last().GetHash().ToStorageKey());
                discardedBranch.BranchKeys
                    .ShouldContain(_kernelTestHelper.LongestBranchBlockList.Last().GetHash().ToStorageKey());

                discardedBranch.NotLinkedKeys.Count.ShouldBe(0);
            }

            {
                //         LIB height: 10
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch: (-)                               (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch: (-)                (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[9]
                    .Height, _kernelTestHelper.BestBranchBlockList[9].GetHash());
                chain = await _fullBlockchainService.GetChainAsync();

                var discardedBranch = await _fullBlockchainService.GetDiscardedBranchAsync(chain);

                discardedBranch.BranchKeys.Count.ShouldBe(2);
                discardedBranch.BranchKeys
                    .ShouldContain(_kernelTestHelper.ForkBranchBlockList.Last().GetHash().ToStorageKey());
                discardedBranch.BranchKeys
                    .ShouldContain(_kernelTestHelper.LongestBranchBlockList.Last().GetHash().ToStorageKey());

                discardedBranch.NotLinkedKeys.Count.ShouldBe(1);
                discardedBranch.NotLinkedKeys.ShouldContain(_kernelTestHelper.NotLinkedBlockList[0].Header
                    .PreviousBlockHash.ToStorageKey());
            }

            {
                //         LIB height: 11
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch: (-)                               (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch: (-)                (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                             v(-) -> w  -> x  -> y  -> z
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[10]
                    .Height, _kernelTestHelper.BestBranchBlockList[10].GetHash());
                chain = await _fullBlockchainService.GetChainAsync();

                var discardedBranch = await _fullBlockchainService.GetDiscardedBranchAsync(chain);

                discardedBranch.BranchKeys.Count.ShouldBe(2);
                discardedBranch.BranchKeys
                    .ShouldContain(_kernelTestHelper.ForkBranchBlockList.Last().GetHash().ToStorageKey());
                discardedBranch.BranchKeys
                    .ShouldContain(_kernelTestHelper.LongestBranchBlockList.Last().GetHash().ToStorageKey());

                discardedBranch.NotLinkedKeys.Count.ShouldBe(2);
                discardedBranch.NotLinkedKeys.ShouldContain(_kernelTestHelper.NotLinkedBlockList[0].Header
                    .PreviousBlockHash.ToStorageKey());
                discardedBranch.NotLinkedKeys.ShouldContain(_kernelTestHelper.NotLinkedBlockList[1].Header
                    .PreviousBlockHash.ToStorageKey());
            }
        }

        [Fact]
        public async Task Get_DiscardedBranch_InvalidBranchTest()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var branchCount = chain.Branches.Count;
            await _fullBlockchainService.AttachBlockToChainAsync(chain, _kernelTestHelper.BestBranchBlockList[3]);

            chain = await _fullBlockchainService.GetChainAsync();
            chain.Branches.Count.ShouldBe(branchCount + 1);

            var discardedBranch = await _fullBlockchainService.GetDiscardedBranchAsync(chain);

            discardedBranch.BranchKeys.Count.ShouldBe(1);
            discardedBranch.BranchKeys.ShouldContain(_kernelTestHelper.BestBranchBlockList[3].GetHash().ToStorageKey());
            discardedBranch.NotLinkedKeys.Count.ShouldBe(0);
        }

        [Fact]
        public async Task Clean_ChainBranch_Test()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var bestChainKey = chain.BestChainHash.ToStorageKey();
            var longestChainKey = chain.LongestChainHash.ToStorageKey();

            var discardedBranch = new DiscardedBranch
            {
                BranchKeys = new List<string>
                {
                    bestChainKey,
                    longestChainKey,
                    _kernelTestHelper.ForkBranchBlockList.Last().GetHash().ToStorageKey(),
                    "Not Exist Branch"
                },
                NotLinkedKeys = new List<string>
                {
                    _kernelTestHelper.NotLinkedBlockList[0].Header.PreviousBlockHash.ToStorageKey(),
                    _kernelTestHelper.NotLinkedBlockList[1].Header.PreviousBlockHash.ToStorageKey(),
                    "Not Exist Block"
                }
            };
            await _fullBlockchainService.CleanChainBranchAsync(discardedBranch);

            var currentChain = await _fullBlockchainService.GetChainAsync();

            currentChain.LongestChainHash.ShouldBe(currentChain.BestChainHash);

            currentChain.Branches.Count.ShouldBe(chain.Branches.Count - 2);
            currentChain.Branches.ShouldNotContainKey(longestChainKey);
            currentChain.Branches.ShouldNotContainKey(_kernelTestHelper.ForkBranchBlockList.Last().GetHash()
                .ToStorageKey());

            currentChain.NotLinkedBlocks.Count.ShouldBe(3);
            currentChain.NotLinkedBlocks.ShouldNotContainKey(discardedBranch.NotLinkedKeys[0]);
            currentChain.NotLinkedBlocks.ShouldNotContainKey(discardedBranch.NotLinkedKeys[1]);
        }

        [Fact]
        public async Task ResetChainToLib_Test()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            chain = await _fullBlockchainService.ResetChainToLibAsync(chain);

            chain.BestChainHash.ShouldBe(chain.LastIrreversibleBlockHash);
            chain.BestChainHeight.ShouldBe(chain.LastIrreversibleBlockHeight);
            chain.LongestChainHash.ShouldBe(chain.LastIrreversibleBlockHash);
            chain.LongestChainHeight.ShouldBe(chain.LastIrreversibleBlockHeight);

            chain.Branches.Count.ShouldBe(1);
            chain.Branches[chain.LastIrreversibleBlockHash.ToStorageKey()].ShouldBe(chain.LastIrreversibleBlockHeight);

            chain.NotLinkedBlocks.ShouldBeEmpty();

            foreach (var block in _kernelTestHelper.LongestBranchBlockList)
            {
                var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
                chainBlockLink.IsLinked.ShouldBeFalse();
                chainBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionNone);
            }

            foreach (var block in _kernelTestHelper.ForkBranchBlockList)
            {
                var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
                chainBlockLink.IsLinked.ShouldBeFalse();
                chainBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionNone);
            }

            foreach (var block in _kernelTestHelper.ForkBranchBlockList.TakeWhile(block =>
                block.Height != chain.LastIrreversibleBlockHeight))
            {
                var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
                chainBlockLink.IsLinked.ShouldBeFalse();
                chainBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionNone);
            }
        }

        [Fact]
        public async Task Transaction_Test()
        {
            var transaction1 = _kernelTestHelper.GenerateTransaction();
            var transaction2 = _kernelTestHelper.GenerateTransaction();

            var hasTransaction = await _fullBlockchainService.HasTransactionAsync(transaction1.GetHash());
            hasTransaction.ShouldBeFalse();
            hasTransaction = await _fullBlockchainService.HasTransactionAsync(transaction2.GetHash());
            hasTransaction.ShouldBeFalse();

            await _fullBlockchainService.AddTransactionsAsync(new List<Transaction> {transaction1, transaction2});

            var transactions = await _fullBlockchainService.GetTransactionsAsync(new List<Hash>
                {transaction1.GetHash(), transaction2.GetHash()});

            transactions[0].ShouldBe(transaction1);
            transactions[1].ShouldBe(transaction2);

            hasTransaction = await _fullBlockchainService.HasTransactionAsync(transaction1.GetHash());
            hasTransaction.ShouldBeTrue();
            hasTransaction = await _fullBlockchainService.HasTransactionAsync(transaction2.GetHash());
            hasTransaction.ShouldBeTrue();
        }
        
        [Fact]
        public async Task RemoveLongestBranch_Test()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var bestChainHash = chain.BestChainHash;
            var bestChainHeight = chain.BestChainHeight;
            chain.LongestChainHash.ShouldNotBe(bestChainHash);
            chain.LongestChainHeight.ShouldNotBe(bestChainHeight);

            await _fullBlockchainService.RemoveLongestBranchAsync(chain);
            
            chain.LongestChainHash.ShouldBe(bestChainHash);
            chain.LongestChainHeight.ShouldBe(bestChainHeight);
        }

    }
}