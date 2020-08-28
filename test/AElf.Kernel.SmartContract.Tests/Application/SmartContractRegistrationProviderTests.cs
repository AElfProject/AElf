using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public sealed class SmartContractRegistrationProviderTests : SmartContractTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly SmartContractHelper _smartContractHelper;

        public SmartContractRegistrationProviderTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractRegistrationProvider = GetRequiredService<ISmartContractRegistrationProvider>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _smartContractHelper = GetRequiredService<SmartContractHelper>();
        }
        
        [Fact]
        public async Task SmartContractRegistrationSetAndGet_Test()
        {
            var chain = await _smartContractHelper.CreateChainAsync();
            var blockExecutedDataKey = $"BlockExecutedData/SmartContractRegistration/{SampleAddress.AddressList[0]}";
            
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            
            var smartContractRegistrationFromProvider =
                await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext,
                    SampleAddress.AddressList[0]);
            smartContractRegistrationFromProvider.ShouldBeNull();
            
            var smartContractRegistration = new SmartContractRegistration
            {
                CodeHash = HashHelper.ComputeFrom(blockExecutedDataKey),
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Version = 1
            };
            await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext,
                SampleAddress.AddressList[0], smartContractRegistration);
            
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            blockStateSet.BlockExecutedData.ShouldContainKey(blockExecutedDataKey);

            smartContractRegistrationFromProvider =
                await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext,
                    SampleAddress.AddressList[0]);
            smartContractRegistrationFromProvider.ShouldBe(smartContractRegistration);
        }
    }
}