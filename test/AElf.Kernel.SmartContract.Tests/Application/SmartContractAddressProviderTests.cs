using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public sealed class SmartContractAddressProviderTests : SmartContractTestBase
    {
        private readonly ISmartContractAddressProvider _smartContractAddressProvider;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly SmartContractHelper _smartContractHelper;

        public SmartContractAddressProviderTests()
        {
            _smartContractAddressProvider = GetRequiredService<ISmartContractAddressProvider>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _smartContractHelper = GetRequiredService<SmartContractHelper>();
        }
        
        [Fact]
        public async Task SmartContractAddress_Set_And_Get_Test()
        {
            var chain = await _smartContractHelper.CreateChainAsync();
            var contractName = Hash.Empty.ToStorageKey();
            
            var chainContext = new ChainContext
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            };
            
            var smartContractAddress =
                await _smartContractAddressProvider.GetSmartContractAddressAsync(chainContext, contractName);
            smartContractAddress.ShouldBeNull();
            
            await _smartContractAddressProvider.SetSmartContractAddressAsync(chainContext, contractName,
                SampleAddress.AddressList[0]);
            
            var blockExecutedDataKey = $"BlockExecutedData/SmartContractAddress/{contractName}";
            var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(chain.BestChainHash);
            blockStateSet.BlockExecutedData.ShouldContainKey(blockExecutedDataKey);

            smartContractAddress =
                await _smartContractAddressProvider.GetSmartContractAddressAsync(chainContext, contractName);
            smartContractAddress.Address.ShouldBe(SampleAddress.AddressList[0]);
            smartContractAddress.BlockHeight = chainContext.BlockHeight;
            smartContractAddress.BlockHash = chainContext.BlockHash;
        }
    }
}