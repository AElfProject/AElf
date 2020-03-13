using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeeCalculation
{
    public class CalculateFunctionExecutedDataServiceTests : TransactionFeeTestBase
    {
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly IBlockchainStateService _blockchainStateService;
        private readonly IBlockchainService _blockchainService;

        private readonly ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
            _calculateFunctionExecutedDataService;

        private readonly KernelTestHelper _kernelTestHelper;

        public CalculateFunctionExecutedDataServiceTests()
        {
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _blockchainStateService = GetRequiredService<IBlockchainStateService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _calculateFunctionExecutedDataService =
                GetRequiredService<ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task CalculateFunctionMap_Test()
        {
            var genesisBlock = _kernelTestHelper.GenerateBlock(0, Hash.Empty, new List<Transaction>());
            var chain = await _blockchainService.CreateChainAsync(genesisBlock, new List<Transaction>());
            var blockStateSet = new BlockStateSet
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);

            var functionMapDict = new Dictionary<string, Dictionary<string, CalculateFunction>>();
            var functionMap = GenerateFunctionMap();
            functionMapDict.Add(GetBlockExecutedDataKey(), functionMap);

            await _calculateFunctionExecutedDataService.AddBlockExecutedDataAsync(new BlockIndex
                {
                    BlockHash = blockStateSet.BlockHash,
                    BlockHeight = blockStateSet.BlockHeight
                },
                functionMapDict);

            var newBlockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            newBlockStateSet.BlockHash.ShouldBe(blockStateSet.BlockHash);
            newBlockStateSet.BlockHeight.ShouldBe(blockStateSet.BlockHeight);
            newBlockStateSet.BlockExecutedData.Count.ShouldBe(1);
            newBlockStateSet.BlockExecutedData.Keys.ShouldContain(key =>
                key.Contains(typeof(AllCalculateFeeCoefficients).Name));

            blockStateSet = await AddBlockStateSetAsync(blockStateSet);
            CheckBlockExecutedData(blockStateSet, functionMap);
            await _blockchainStateService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
            CheckBlockExecutedData(blockStateSet, functionMap);

            blockStateSet = await AddBlockStateSetAsync(blockStateSet);
            CheckBlockExecutedData(blockStateSet, functionMap);
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

        private Dictionary<string, CalculateFunction> GenerateFunctionMap()
        {
            return new Dictionary<string, CalculateFunction>
            {
                {
                    "TX", new CalculateFunction(4)
                    {
                        CalculateFeeCoefficients = new CalculateFeeCoefficients
                        {
                            PieceCoefficientsList =
                            {
                                new CalculateFeePieceCoefficients
                                {
                                    Value = {10, 1, 2, 3}
                                }
                            }
                        }
                    }
                },
                {
                    "STORAGE", new CalculateFunction(1)
                    {
                        CalculateFeeCoefficients = new CalculateFeeCoefficients
                        {
                            PieceCoefficientsList =
                            {
                                new CalculateFeePieceCoefficients
                                {
                                    Value = {100, 1, 2, 3}
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}