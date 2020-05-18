﻿using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.ContractTestBase.Tests
{
    public class MainChainTests : MainChainTestBase
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
                contractMapping[TokenSmartContractAddressNameProvider.Name], SampleECKeyPairs.KeyPairs[0]);
            var balance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
                Symbol = "ELF"
            });
            balance.Balance.ShouldBe(88000000000000000L);

            var electionStub = GetTester<ElectionContractContainer.ElectionContractStub>(
                contractMapping[ElectionSmartContractAddressNameProvider.Name], SampleECKeyPairs.KeyPairs[0]);
            var minerCount = await electionStub.GetMinersCount.CallAsync(new Empty());
            minerCount.Value.ShouldBe(1);
        }
    }
}