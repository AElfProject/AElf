using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.TestBase;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public sealed class NonparallelContractCodeProviderTests : AElfIntegratedTest<ParallelExecutionTestModule>
    {
        private readonly IBlockchainService _blockchainService;
        private readonly INonparallelContractCodeProvider _nonparallelContractCodeProvider;
        private readonly IBlockStateSetManger _blockStateSetManger;

        public NonparallelContractCodeProviderTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _nonparallelContractCodeProvider = GetRequiredService<INonparallelContractCodeProvider>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        }
        
        [Fact]
        public async Task NonparallelContractCodeSetAndGet_Test()
        {
            var chain = await _blockchainService.GetChainAsync();

            var blockExecutedDataKey = $"BlockExecutedData/NonparallelContractCode/{SampleAddress.AddressList[0]}";
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            blockStateSet.BlockExecutedData.ShouldNotContainKey(blockExecutedDataKey);
            
            var nonparallelContractCode = new NonparallelContractCode
            {
                CodeHash = HashHelper.ComputeFrom(blockExecutedDataKey)
            };

            var dictionary = new Dictionary<Address, NonparallelContractCode>
            {
                {SampleAddress.AddressList[0], nonparallelContractCode}
            };

            await _nonparallelContractCodeProvider.SetNonparallelContractCodeAsync(new BlockIndex
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            }, dictionary);
            
            blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            blockStateSet.BlockExecutedData.ShouldContainKey(blockExecutedDataKey);

            var nonparallelContractCodeFromState = await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(
                new ChainContext
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, SampleAddress.AddressList[0]);
            nonparallelContractCodeFromState.ShouldBe(nonparallelContractCode);
        }
    }
}