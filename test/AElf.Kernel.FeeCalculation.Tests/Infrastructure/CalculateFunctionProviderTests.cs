using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{ 
    public sealed class CalculateFunctionProviderTests : TransactionFeeTestBase
    {
        private readonly ICalculateFunctionProvider _calculateFunctionProvider;

        public CalculateFunctionProviderTests()
        {
            _calculateFunctionProvider =GetRequiredService<ICalculateFunctionProvider>();
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

            var blockExecutedDataKey = "BlockExecutedData/AllCalculateFeeCoefficients";
            blockStateSet.BlockExecutedData.ShouldNotContainKey(blockExecutedDataKey);
            var functionMap = GenerateFunctionMap();
            await _calculateFunctionProvider.AddCalculateFunctions(new BlockIndex
                {
                    BlockHash = blockStateSet.BlockHash,
                    BlockHeight = blockStateSet.BlockHeight
                },
                functionMap);

            var newBlockStateSet = await BlockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            newBlockStateSet.BlockHash.ShouldBe(blockStateSet.BlockHash);
            newBlockStateSet.BlockHeight.ShouldBe(blockStateSet.BlockHeight);
            newBlockStateSet.BlockExecutedData.Count.ShouldBe(1);
            newBlockStateSet.BlockExecutedData.ShouldContainKey(blockExecutedDataKey);

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
                _calculateFunctionProvider.GetCalculateFunctions(chainContext);
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