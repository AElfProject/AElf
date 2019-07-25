using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceTests : AElfKernelWithChainTestBase
    {
        private readonly FullBlockchainService _fullBlockchainService;
        private readonly ITransactionManager _transactionManager;
        private readonly IChainManager _chainManager;
        private readonly KernelTestHelper _kernelTestHelper;

        public FullBlockchainServiceTests()
        {
            _fullBlockchainService = GetRequiredService<FullBlockchainService>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _chainManager = GetRequiredService<IChainManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
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
            await _fullBlockchainService.AddTransactionsAsync(transactions);

            existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.GetHash().ShouldBe(block.GetHash());
            existBlock.Body.TransactionsCount.ShouldBe(3);

            foreach (var tx in transactions)
            {
                var existTransaction = await _transactionManager.GetTransaction(tx.GetHash());
                existTransaction.ShouldBe(tx);
            }
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

            result = await _fullBlockchainService.HasBlockAsync(_kernelTestHelper.UnlinkedBranchBlockList[1].GetHash());
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
            var chain = await _fullBlockchainService.GetChainAsync();

            var result =
                await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 14, chain.BestChainHash);
            result.ShouldBeNull();

            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 14, chain.LongestChainHash);
            result.ShouldBeNull();

            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 14,
                _kernelTestHelper.ForkBranchBlockList.Last().GetHash());
            result.ShouldBeNull();

            result = await _fullBlockchainService.GetBlockHashByHeightAsync(chain, 15,
                _kernelTestHelper.UnlinkedBranchBlockList.Last().GetHash());
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
            var chain = await _fullBlockchainService.GetChainAsync();

            chain.BestChainHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList.Last().Height);
            chain.BestChainHash.ShouldBe(_kernelTestHelper.BestBranchBlockList.Last().GetHash());

            await _fullBlockchainService.SetBestChainAsync(chain, chain.LongestChainHeight, chain.LongestChainHash);

            chain = await _fullBlockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(_kernelTestHelper.LongestBranchBlockList.Last().Height);
            chain.BestChainHash.ShouldBe(_kernelTestHelper.LongestBranchBlockList.Last().GetHash());
        }

        [Fact]
        public async Task Get_GetReservedBlockHashes_ReturnNull()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            var result = await _fullBlockchainService.GetReversedBlockIndexes(Hash.FromString("not exist"), 1);
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
            result[0].Hash.ShouldBe(_kernelTestHelper.BestBranchBlockList[4].GetHash());
            result[1].Hash.ShouldBe(_kernelTestHelper.BestBranchBlockList[3].GetHash());
            result[2].Hash.ShouldBe(_kernelTestHelper.BestBranchBlockList[2].GetHash());

            result = await _fullBlockchainService.GetReversedBlockIndexes(
                _kernelTestHelper.BestBranchBlockList[3].GetHash(), 4);
            result.Count.ShouldBe(3);
            result[0].Hash.ShouldBe(_kernelTestHelper.BestBranchBlockList[2].GetHash());
            result[1].Hash.ShouldBe(_kernelTestHelper.BestBranchBlockList[1].GetHash());
            result[2].Hash.ShouldBe(chain.GenesisBlockHash);
        }

        [Fact]
        public async Task Get_Blocks_ReturnEmpty()
        {
            var result = await _fullBlockchainService.GetBlocksInBestChainBranchAsync(Hash.FromString("not exist"), 3);
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
            var notExistHash = Hash.FromString("not exist");

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
                    _kernelTestHelper.UnlinkedBranchBlockList.Last().GetHash())
                .ShouldThrowAsync<InvalidOperationException>();
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
            var result = await _fullBlockchainService.GetBlockByHashAsync(Hash.FromString("Not Exist Block"));
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

            var newBlock = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, Hash.FromString("New Branch"));

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
                _kernelTestHelper.LongestBranchBlockList[0].GetHash(), 10);
            result.Count.ShouldBe(4);
            result[0].GetHash().ShouldBe(_kernelTestHelper.LongestBranchBlockList[1].GetHash());
            result[1].GetHash().ShouldBe(_kernelTestHelper.LongestBranchBlockList[2].GetHash());
            result[2].GetHash().ShouldBe(_kernelTestHelper.LongestBranchBlockList[3].GetHash());
            result[3].GetHash().ShouldBe(_kernelTestHelper.LongestBranchBlockList[4].GetHash());
        }

        [Fact]
        public async Task Set_IrreversibleBlock_Test()
        {
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
                chain.Branches.Count.ShouldBe(3);
                chain.NotLinkedBlocks.Count.ShouldBe(5);
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.UnlinkedBranchBlockList.Select(b => b.GetHash()).ToList());
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
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[8].GetHash());
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[8].Height);
                chain.Branches.Count.ShouldBe(2);
                chain.NotLinkedBlocks.Count.ShouldBe(5);
                BlocksShouldNotExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.UnlinkedBranchBlockList.Select(b => b.GetHash()).ToList());
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
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[9].GetHash());
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[9].Height);
                chain.Branches.Count.ShouldBe(1);
                chain.NotLinkedBlocks.Count.ShouldBe(5);
                BlocksShouldNotExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.UnlinkedBranchBlockList.Select(b => b.GetHash()).ToList());
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
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].GetHash());
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].Height);
                chain.Branches.Count.ShouldBe(1);
                chain.NotLinkedBlocks.Count.ShouldBe(4);
                chain.LongestChainHash.ShouldBe(chain.BestChainHash);
                chain.LongestChainHeight.ShouldBe(chain.BestChainHeight);
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(new List<Hash> {_kernelTestHelper.UnlinkedBranchBlockList[0].GetHash()});
                BlocksShouldExist(new List<Hash>
                {
                    _kernelTestHelper.UnlinkedBranchBlockList[1].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[2].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[3].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[4].GetHash()
                });
            }
        }

        [Fact]
        public async Task Set_IrreversibleBlock_HigherThanBranch()
        {
            var newBlock1 = await _kernelTestHelper.AttachBlock(_kernelTestHelper.BestBranchBlockList[5].Height,
                _kernelTestHelper
                    .BestBranchBlockList[5].GetHash());
            var newBlock2 = await _kernelTestHelper.AttachBlock(newBlock1.Height, newBlock1.GetHash());

            var chain = await _fullBlockchainService.GetChainAsync();
            {
                //         LIB height: 10
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch:                                   (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch:                    (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                              v  -> w  -> x  -> y  -> z
                //    New Fork Branch:                         (f)-> aa-> ab     
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[9]
                    .Height, _kernelTestHelper.BestBranchBlockList[9].GetHash());

                chain = await _fullBlockchainService.GetChainAsync();
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[9].GetHash());
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[9].Height);
                chain.Branches.Count.ShouldBe(4);
                chain.NotLinkedBlocks.Count.ShouldBe(5);
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.UnlinkedBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(new List<Hash> {newBlock1.GetHash(), newBlock2.GetHash()});
            }
            {
                //         LIB height: 11
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch: (-)                               (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch: (-)                (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                             v(-) -> w  -> x  -> y  -> z
                //    New Fork Branch: (-)                     (f)-> aa-> ab     
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[10]
                    .Height, _kernelTestHelper.BestBranchBlockList[10].GetHash());

                chain = await _fullBlockchainService.GetChainAsync();
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].GetHash());
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].Height);
                chain.Branches.Count.ShouldBe(1);
                chain.NotLinkedBlocks.Count.ShouldBe(4);
                chain.LongestChainHash.ShouldBe(chain.BestChainHash);
                chain.LongestChainHeight.ShouldBe(chain.BestChainHeight);
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(new List<Hash> {_kernelTestHelper.UnlinkedBranchBlockList[0].GetHash()});
                BlocksShouldExist(new List<Hash>
                {
                    _kernelTestHelper.UnlinkedBranchBlockList[1].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[2].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[3].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[4].GetHash()
                });
                BlocksShouldNotExist(new List<Hash> {newBlock1.GetHash(), newBlock2.GetHash()});
            }
        }

        [Fact]
        public async Task Set_IrreversibleBlock_Concurrence()
        {
            var chain = await _fullBlockchainService.GetChainAsync();

            //         LIB height: 8
            //
            //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
            //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
            //     Longest Branch:                                   (h)-> l -> m  -> n  -> o  -> p 
            //        Fork Branch:                    (e)-> q -> r -> s -> t -> u
            //    Unlinked Branch:                                    ae   v -> w  -> x  -> y  -> z
            await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[7]
                .Height, _kernelTestHelper.BestBranchBlockList[7].GetHash());
            
            var newUnlinkedBlock = _kernelTestHelper.GenerateBlock(7, Hash.FromString("NewUnlinked"),
                new List<Transaction> {_kernelTestHelper.GenerateTransaction()});
            await _fullBlockchainService.AddBlockAsync(newUnlinkedBlock);
            await _fullBlockchainService.AttachBlockToChainAsync(chain, newUnlinkedBlock);

            var previousBlockHeight = _kernelTestHelper.BestBranchBlockList.Last().Height;
            var previousBlockHash = _kernelTestHelper.BestBranchBlockList.Last().GetHash();

            // Miner mined one block
            var minerAttachBlock = await _kernelTestHelper.AttachBlock(previousBlockHeight, previousBlockHash);
            chain = await _fullBlockchainService.GetChainAsync();
            await _fullBlockchainService.SetBestChainAsync(chain, minerAttachBlock.Height,
                minerAttachBlock.GetHash());
            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(minerAttachBlock.GetHash());
            await _chainManager.SetChainBlockLinkExecutionStatus(chainBlockLink,
                ChainBlockLinkExecutionStatus.ExecutionSuccess);
            var minerAttachChain = await _fullBlockchainService.GetChainAsync();

            // Network sync two blocks
            Block syncAttachBlock = null;
            for (var i = 0; i < 2; i++)
            {
                syncAttachBlock = await _kernelTestHelper.AttachBlock(previousBlockHeight, previousBlockHash);
                chain = await _fullBlockchainService.GetChainAsync();
                await _fullBlockchainService.SetBestChainAsync(chain, syncAttachBlock.Height,
                    syncAttachBlock.GetHash());
                chainBlockLink = await _chainManager.GetChainBlockLinkAsync(syncAttachBlock.GetHash());
                await _chainManager.SetChainBlockLinkExecutionStatus(chainBlockLink,
                    ChainBlockLinkExecutionStatus.ExecutionSuccess);

                previousBlockHeight = syncAttachBlock.Height;
                previousBlockHash = syncAttachBlock.GetHash();
            }
            var syncAttachChain = await _fullBlockchainService.GetChainAsync();

            {
                //         LIB height: 9
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch: (-)                               (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch: (-)                (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                   ae(-) v  -> w  -> x  -> y  -> z
                //       Miner Branch:                                                    (k) -> aa
                //Network Sync Branch:                                                    (k) -> ab -> ac    
                syncAttachChain.NotLinkedBlocks.Count.ShouldBe(6);
                BlocksShouldExist(new List<Hash> {newUnlinkedBlock.GetHash()});

                await _fullBlockchainService.SetIrreversibleBlockAsync(syncAttachChain, _kernelTestHelper
                    .BestBranchBlockList[8].Height, _kernelTestHelper.BestBranchBlockList[8].GetHash());

                chain = await _fullBlockchainService.GetChainAsync();
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[8].GetHash());
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[8].Height);
                chain.Branches.Count.ShouldBe(3);
                chain.NotLinkedBlocks.Count.ShouldBe(5);
                BlocksShouldNotExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.UnlinkedBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(new List<Hash> {newUnlinkedBlock.GetHash()});
            }
            
            {
                //         LIB height: 10
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch: (-)                               (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch: (-)                (e)-> q -> r -> s -> t -> u  -> ad
                //    Unlinked Branch:                                   ae(-) v  -> w  -> x  -> y  -> z
                //       Miner Branch:                                                    (k) -> aa
                //Network Sync Branch: (-)                                                (k) -> ab -> ac 
                var newBlock = _kernelTestHelper.GenerateBlock(_kernelTestHelper.ForkBranchBlockList.Last().Height,
                    _kernelTestHelper.ForkBranchBlockList.Last().GetHash(),
                    new List<Transaction> {_kernelTestHelper.GenerateTransaction()});
                await _fullBlockchainService.AddBlockAsync(newBlock);
                await _fullBlockchainService.AttachBlockToChainAsync(minerAttachChain, newBlock);
                minerAttachChain.Branches.Count.ShouldBe(3);
                
                await _fullBlockchainService.SetIrreversibleBlockAsync(minerAttachChain, _kernelTestHelper
                    .BestBranchBlockList[9].Height, _kernelTestHelper.BestBranchBlockList[9].GetHash());
                
                chain = await _fullBlockchainService.GetChainAsync();
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[9].Height);
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[9].GetHash());
                chain.Branches.Count.ShouldBe(2);
                chain.NotLinkedBlocks.Count.ShouldBe(5);
                BlocksShouldNotExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.UnlinkedBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(new List<Hash>{newBlock.GetHash()});
            }
            
            {
                //         LIB height: 11
                //
                //             Height: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12 -> 13 -> 14
                //        Best Branch: a -> b -> c -> d -> e -> f -> g -> h -> i -> j  -> k
                //     Longest Branch: (-)                               (h)-> l -> m  -> n  -> o  -> p 
                //        Fork Branch: (-)                (e)-> q -> r -> s -> t -> u
                //    Unlinked Branch:                                   ae(-) v(-)-> w  -> x  -> y  -> z
                //       Miner Branch:                                                    (k) -> aa
                //Network Sync Branch: (-)                                                (k) -> ab -> ac 
                
                await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper
                    .BestBranchBlockList[10].Height, _kernelTestHelper.BestBranchBlockList[10].GetHash());

                chain = await _fullBlockchainService.GetChainAsync();
                chain.LastIrreversibleBlockHeight.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].Height);
                chain.LastIrreversibleBlockHash.ShouldBe(_kernelTestHelper.BestBranchBlockList[10].GetHash());
                chain.LongestChainHash.ShouldBe(minerAttachBlock.GetHash());
                chain.LongestChainHeight.ShouldBe(minerAttachBlock.Height);
                chain.Branches.Count.ShouldBe(1);
                chain.NotLinkedBlocks.Count.ShouldBe(4);
                BlocksShouldNotExist(_kernelTestHelper.ForkBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldExist(_kernelTestHelper.BestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(_kernelTestHelper.LongestBranchBlockList.Select(b => b.GetHash()).ToList());
                BlocksShouldNotExist(new List<Hash> {_kernelTestHelper.UnlinkedBranchBlockList[0].GetHash()});
                BlocksShouldExist(new List<Hash>
                {
                    _kernelTestHelper.UnlinkedBranchBlockList[1].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[2].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[3].GetHash(),
                    _kernelTestHelper.UnlinkedBranchBlockList[4].GetHash()
                });
            }
        }

        [Fact]
        public async Task Attach_New_Block_To_Chain_Test()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            var newUnlinkedBlock = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash,
                new List<Transaction> {_kernelTestHelper.GenerateTransaction()});
            await _fullBlockchainService.AddBlockAsync(newUnlinkedBlock);
            var status = await _fullBlockchainService.AttachBlockToChainAsync(chain, newUnlinkedBlock);
            status.ShouldBe(BlockAttachOperationStatus.NewBlockLinked);
        }
        
        [Fact]
        public async Task Attach_Linked_Block_To_Chain_Test()
        {
            var chain = await _fullBlockchainService.GetChainAsync();
            await _fullBlockchainService.SetIrreversibleBlockAsync(chain, _kernelTestHelper.BestBranchBlockList[7]
                .Height, _kernelTestHelper.BestBranchBlockList[7].GetHash());
            var linkedBlock = _kernelTestHelper.BestBranchBlockList[8];
            await _fullBlockchainService.AddBlockAsync(linkedBlock);
            var status = await _fullBlockchainService.AttachBlockToChainAsync(chain, linkedBlock);
            status.ShouldBe(BlockAttachOperationStatus.NewBlockLinked);
        }

        private void BlocksShouldNotExist(List<Hash> blockHashes)
        {
            foreach (var hash in blockHashes)
            {
                var block = _fullBlockchainService.GetBlockByHashAsync(hash).Result;
                block.ShouldBeNull();
                var blockLink = _chainManager.GetChainBlockLinkAsync(hash).Result;
                blockLink.ShouldBeNull();
            }
        }

        private void BlocksShouldExist(List<Hash> blockHashes)
        {
            foreach (var hash in blockHashes)
            {
                var block = _fullBlockchainService.GetBlockByHashAsync(hash).Result;
                block.ShouldNotBeNull();
                block.GetHash().ShouldBe(hash);

                var blockLink = _chainManager.GetChainBlockLinkAsync(hash).Result;
                blockLink.ShouldNotBeNull();
                blockLink.BlockHash.ShouldBe(hash);
            }
        }
    }
}
