using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.ContractTestBase.Tests
{
    public sealed class MainChainTests : MainChainTestBase
    {
        private readonly IBlockchainService _blockchainService;
        
        public MainChainTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task Test()
        {
            var preBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var chainContext = new ChainContext
            {
                BlockHash = preBlockHeader.GetHash(),
                BlockHeight = preBlockHeader.Height
            };
            var contractMapping = await ContractAddressService.GetSystemContractNameToAddressMappingAsync(chainContext);
            
            var tokenStub = GetTester<TokenContractContainer.TokenContractStub>(
                contractMapping[TokenSmartContractAddressNameProvider.Name], Accounts[0].KeyPair);
            var balance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Accounts[0].Address,
                Symbol = "ELF"
            });
            balance.Balance.ShouldBe(88000000000000000L);

            var electionStub = GetTester<ElectionContractContainer.ElectionContractStub>(
                contractMapping[ElectionSmartContractAddressNameProvider.Name], Accounts[0].KeyPair);
            var minerCount = await electionStub.GetMinersCount.CallAsync(new Empty());
            minerCount.Value.ShouldBe(1);
        }
    }
}