using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Domain
{
    public sealed class BlockchainExecutedDataManagerTests : AElfKernelTestBase
    {
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly IBlockchainStateService _blockchainStateService;
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainExecutedDataManager _blockchainExecutedDataManager;
        private readonly KernelTestHelper _kernelTestHelper;

        public BlockchainExecutedDataManagerTests()
        {
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _blockchainStateService = GetRequiredService<IBlockchainStateService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockchainExecutedDataManager = GetRequiredService<IBlockchainExecutedDataManager>();
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
            
            var chainKey = GetBlockExecutedDataKey<Chain>();
            var dictionary = new Dictionary<string, ByteString>
            {
                {chainKey, ByteString.CopyFrom(SerializationHelper.Serialize(chain))}
            };
            await _blockchainExecutedDataManager.AddBlockExecutedCacheAsync(blockStateSet.BlockHash, dictionary);
            var isInStore = await CheckExecutedDataInStoreAsync(blockStateSet, chainKey, dictionary[chainKey]);
            isInStore.ShouldBeFalse();
            
            await _blockchainStateService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
            isInStore = await CheckExecutedDataInStoreAsync(blockStateSet, chainKey, dictionary[chainKey]);
            isInStore.ShouldBeTrue();
            
            blockStateSet = await AddBlockStateSetAsync(blockStateSet);
           
            isInStore = await CheckExecutedDataInStoreAsync(blockStateSet, chainKey, dictionary[chainKey]);
            isInStore.ShouldBeTrue();
            
            // BlockStateSet is not exist
            var notExistHash = HashHelper.ComputeFrom("NotExist");
            await _blockchainExecutedDataManager.AddBlockExecutedCacheAsync(notExistHash, dictionary);
            var stateSet = await _blockStateSetManger.GetBlockStateSetAsync(notExistHash);
            stateSet.ShouldBeNull();
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

        private async Task<bool> CheckExecutedDataInStoreAsync(BlockStateSet blockStateSet, string key,
            ByteString blockExecutedData)
        {
            var stateReturn = await _blockchainExecutedDataManager.GetExecutedCacheAsync(key, blockStateSet.BlockHeight,
                blockStateSet.BlockHash);
            stateReturn.Value.ShouldBe(blockExecutedData);
            return stateReturn.IsInStore;
        }
    }
}