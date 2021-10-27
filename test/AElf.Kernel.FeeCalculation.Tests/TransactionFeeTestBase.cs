using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Kernel.FeeCalculation
{
    public class TransactionFeeTestBase : AElfIntegratedTest<KernelTransactionFeeTestAElfModule>
    {
        protected readonly IBlockStateSetManger BlockStateSetManger;
        protected readonly IBlockchainStateService BlockchainStateService;
        protected readonly IBlockchainService BlockchainService;
        protected readonly KernelTestHelper KernelTestHelper;

        protected TransactionFeeTestBase()
        {
            var serviceProvider = Application.ServiceProvider;
            BlockStateSetManger = serviceProvider.GetRequiredService<IBlockStateSetManger>();
            BlockchainStateService = serviceProvider.GetRequiredService<IBlockchainStateService>();
            BlockchainService = serviceProvider.GetRequiredService<IBlockchainService>();
            KernelTestHelper = serviceProvider.GetRequiredService<KernelTestHelper>();
        }
        
        protected string GetBlockExecutedDataKey()
        {
            var list = new List<string> {KernelConstants.BlockExecutedDataKey, nameof(AllCalculateFeeCoefficients)};
            return string.Join("/", list);
        }
        
        protected async Task<BlockStateSet> AddBlockStateSetAsync(BlockStateSet previousBlockStateSet)
        {
            var block = await KernelTestHelper.AttachBlockToBestChain();
            var blockStateSet = new BlockStateSet
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height,
                PreviousHash = previousBlockStateSet.BlockHash
            };
            await BlockStateSetManger.SetBlockStateSetAsync(blockStateSet);
            return blockStateSet;
        }

        protected Dictionary<string, CalculateFunction> GenerateFunctionMap()
        {
            return new Dictionary<string, CalculateFunction>
            {
                {
                    "TX", new CalculateFunction(4)
                    {
                        CalculateFeeCoefficients = new CalculateFeeCoefficients
                        {
                            FeeTokenType = 4,
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
                            FeeTokenType = 1,
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
        private const decimal Precision = 100000000;
        protected long GetFeeForTx(int count)
        {
            count = count > 10 ? 10 : count;
            return (long) ((decimal) Math.Pow(count,1) * 2 / 3 * Precision);
        }
    }
}