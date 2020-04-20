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
        private readonly KernelTestHelper _kernelTestHelper;

        public SmartContractRegistrationProviderTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _smartContractRegistrationProvider = GetRequiredService<ISmartContractRegistrationProvider>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }
        
        [Fact]
        public async Task SmartContractRegistrationSetAndGet_Test()
        {
            var genesisBlock = _kernelTestHelper.GenerateBlock(0, Hash.Empty, new List<Transaction>());
            var chain = await _blockchainService.CreateChainAsync(genesisBlock, new List<Transaction>());
            var blockStateSet = new BlockStateSet
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);
            var blockExecutedDataKey = $"BlockExecutedData/SmartContractRegistration/{SampleAddress.AddressList[0]}";
            blockStateSet.BlockExecutedData.ShouldNotContainKey(blockExecutedDataKey);
            
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            
            var smartContractRegistration = new SmartContractRegistration
            {
                CodeHash = HashHelper.ComputeFromString(blockExecutedDataKey),
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Version = 1
            };
            await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext,
                SampleAddress.AddressList[0], smartContractRegistration);
            
            blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            blockStateSet.BlockExecutedData.ShouldContainKey(blockExecutedDataKey);

            var smartContractRegistrationFromState =
                await _smartContractRegistrationProvider.GetSmartContractRegistrationAsync(chainContext,
                    SampleAddress.AddressList[0]);
            smartContractRegistrationFromState.ShouldBe(smartContractRegistration);
        }
    }
}