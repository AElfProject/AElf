using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeeCalculation
{
    public sealed class CalculateFunctionExecutedDataServiceTests : TransactionFeeTestBase
    {
        private readonly ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
            _calculateFunctionExecutedDataService;

        public CalculateFunctionExecutedDataServiceTests()
        {
            _calculateFunctionExecutedDataService =
                GetRequiredService<ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>>();
        }

        [Fact]
        public async Task CalculateFunctionMap_Test()
        {
            var genesisBlock = KernelTestHelper.GenerateBlock(0, Hash.Empty, new List<Transaction>());
            var chain = await BlockchainService.CreateChainAsync(genesisBlock, new List<Transaction>());
            var blockStateSet = new BlockStateSet
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            await BlockStateSetManger.SetBlockStateSetAsync(blockStateSet);

            var functionMapDict = new Dictionary<string, Dictionary<string, CalculateFunction>>();
            var functionMap = GenerateFunctionMap();
            functionMapDict.Add(GetBlockExecutedDataKey(), functionMap);

            await _calculateFunctionExecutedDataService.AddBlockExecutedDataAsync(new BlockIndex
                {
                    BlockHash = blockStateSet.BlockHash,
                    BlockHeight = blockStateSet.BlockHeight
                },
                functionMapDict);

            var newBlockStateSet = await BlockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            newBlockStateSet.BlockHash.ShouldBe(blockStateSet.BlockHash);
            newBlockStateSet.BlockHeight.ShouldBe(blockStateSet.BlockHeight);
            newBlockStateSet.BlockExecutedData.Count.ShouldBe(1);
            newBlockStateSet.BlockExecutedData.Keys.ShouldContain(key =>
                key.Contains(typeof(AllCalculateFeeCoefficients).Name));

            blockStateSet = await AddBlockStateSetAsync(blockStateSet);
            CheckBlockExecutedData(blockStateSet, functionMap);
            await BlockchainStateService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
            CheckBlockExecutedData(blockStateSet, functionMap);

            blockStateSet = await AddBlockStateSetAsync(blockStateSet);
            CheckBlockExecutedData(blockStateSet, functionMap);
        }

        private void CheckBlockExecutedData(BlockStateSet blockStateSet,
            Dictionary<string, CalculateFunction> functionMap)
        {
            var chainContext = new ChainContext
            {
                BlockHash = blockStateSet.BlockHash,
                BlockHeight = blockStateSet.BlockHeight
            };
            var functionMapFromBlockExecutedData =
                _calculateFunctionExecutedDataService.GetBlockExecutedData(chainContext,
                    GetBlockExecutedDataKey());
            foreach (var key in functionMap.Keys)
            {
                var fromExecutedData = functionMapFromBlockExecutedData.Values.Single(d =>
                    ((FeeTypeEnum) d.CalculateFeeCoefficients.FeeTokenType).ToString().ToUpper() == key);
                var actual = functionMap.Values.Single(d =>
                    ((FeeTypeEnum) d.CalculateFeeCoefficients.FeeTokenType).ToString().ToUpper() == key);
                fromExecutedData.CalculateFeeCoefficients.ShouldBe(actual.CalculateFeeCoefficients);
            }
        }
    }
}