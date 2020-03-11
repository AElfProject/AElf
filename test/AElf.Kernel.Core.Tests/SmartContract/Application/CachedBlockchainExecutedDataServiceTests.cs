using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public sealed class CachedBlockchainExecutedDataServiceTests : AElfKernelTestBase
    {
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly IBlockchainStateService _blockchainStateService;
        private readonly IBlockchainService _blockchainService;
        private readonly ICachedBlockchainExecutedDataService<Chain> _chainBlockchainExecutedDataService;
        private readonly ICachedBlockchainExecutedDataService<Transaction> _transactionBlockchainExecutedDataService;
        private readonly ICachedBlockchainExecutedDataService<TransactionResult> _transactionResultBlockchainExecutedDataService;
        private readonly KernelTestHelper _kernelTestHelper;

        public CachedBlockchainExecutedDataServiceTests()
        {
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _blockchainStateService = GetRequiredService<IBlockchainStateService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _chainBlockchainExecutedDataService = GetRequiredService<ICachedBlockchainExecutedDataService<Chain>>();
            _transactionBlockchainExecutedDataService =
                GetRequiredService<ICachedBlockchainExecutedDataService<Transaction>>();
            _transactionResultBlockchainExecutedDataService =
                GetRequiredService<ICachedBlockchainExecutedDataService<TransactionResult>>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task BlockExecutedData_Test()
        {
            var genesisBlock = _kernelTestHelper.GenerateBlock(0, Hash.Empty, new List<Transaction>());
            var chain = await _blockchainService.CreateChainAsync(genesisBlock, new List<Transaction>());
            var blockStateSet = new BlockStateSet
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);
            
            var transactionDic = new Dictionary<string, Transaction>();
            for (var i = 0; i < 5; i++)
            {
                var transaction = new Transaction
                {
                    From = SampleAddress.AddressList[i],
                    To = SampleAddress.AddressList[i + 1],
                    RefBlockNumber = chain.BestChainHeight - 1,
                    MethodName = "Test"
                };
                transactionDic.Add(GetBlockExecutedDataKey<Transaction>(transaction.GetHash()), transaction);
            }

            await _transactionBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockStateSet.BlockHash,
                transactionDic);
            
            var transactionResult = new TransactionResult
            {
                TransactionId = transactionDic.First().Value.GetHash()
            };
            var transactionResultKey = GetBlockExecutedDataKey<TransactionResult>(transactionResult.TransactionId);
            await _transactionResultBlockchainExecutedDataService.AddBlockExecutedDataAsync(chain.BestChainHash,
                transactionResultKey,
                transactionResult);
            var chainKey = GetBlockExecutedDataKey<Chain>();
            await _chainBlockchainExecutedDataService.AddBlockExecutedDataAsync(blockStateSet.BlockHash,
                chainKey, chain);

            var newBlockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            newBlockStateSet.BlockHash.ShouldBe(blockStateSet.BlockHash);
            newBlockStateSet.BlockHeight.ShouldBe(blockStateSet.BlockHeight);
            newBlockStateSet.BlockExecutedData.Count.ShouldBe(7);
            newBlockStateSet.BlockExecutedData.Keys.ShouldContain(key=>key.Contains(typeof(Transaction).Name));
            newBlockStateSet.BlockExecutedData.Keys.ShouldContain(key=>key.Contains(typeof(TransactionResult).Name));
            newBlockStateSet.BlockExecutedData.Keys.ShouldContain(key=>key.Contains(typeof(Chain).Name));
            
            blockStateSet = await AddBlockStateSetAsync(blockStateSet);
            CheckBlockExecutedData(blockStateSet, chain, transactionResult, transactionDic);
            await _blockchainStateService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
            CheckBlockExecutedData(blockStateSet, chain, transactionResult, transactionDic);
            
            blockStateSet = await AddBlockStateSetAsync(blockStateSet);
            CheckBlockExecutedData(blockStateSet, chain, transactionResult, transactionDic);
        }

        private async Task<BlockStateSet> AddBlockStateSetAsync(BlockStateSet previousBlockStateSet)
        {
            var block = await _kernelTestHelper.AttachBlockToBestChain();
            var blockStateSet = new BlockStateSet
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height,
                PreviousHash = previousBlockStateSet.BlockHash
            };
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);
            return blockStateSet;
        }

        private void CheckBlockExecutedData(BlockStateSet blockStateSet, Chain chain,
            TransactionResult transactionResult, Dictionary<string, Transaction> transactionDic)
        {
            var chainContext = new ChainContext
            {
                BlockHash = blockStateSet.BlockHash,
                BlockHeight = blockStateSet.BlockHeight
            };
            var chainFromBlockExecutedData =
                _chainBlockchainExecutedDataService.GetBlockExecutedData(chainContext,
                    GetBlockExecutedDataKey<Chain>());
            chainFromBlockExecutedData.ShouldBe(chain);

            var transactionResultFromBlockExecutedData =
                _transactionResultBlockchainExecutedDataService.GetBlockExecutedData(chainContext,
                    GetBlockExecutedDataKey<TransactionResult>(transactionResult.TransactionId));
            transactionResultFromBlockExecutedData.ShouldBe(transactionResult);
            foreach (var keyPair in transactionDic)
            {
                var transaction =
                    _transactionBlockchainExecutedDataService.GetBlockExecutedData(chainContext, keyPair.Key);
                transaction.ShouldBe(keyPair.Value);
            }
        }
    }
}