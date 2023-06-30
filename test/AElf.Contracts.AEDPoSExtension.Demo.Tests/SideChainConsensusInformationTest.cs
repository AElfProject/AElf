using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Standards.ACS10;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests;

public class SideChainConsensusInformationTest : AEDPoSExtensionDemoTestBase
{
    [Fact]
    public async Task SideChainDividendPoolTest()
    {
        InitialContracts();

        await ConsensusStub.SetSymbolList.SendWithExceptionAsync(new SymbolList());
        await ConsensusStub.GetDividends.CallAsync(new Int64Value { Value = 1 });
    }

    [Fact]
    public async Task UpdateInformationFromCrossChainTest()
    {
        SetToSideChain();
        InitialContracts();
        InitialAcs3Stubs();
        var mockedCrossChain = SampleAccount.Accounts.Last();
        var mockedCrossChainStub =
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                mockedCrossChain.KeyPair);

        var headerInformation = new AElfConsensusHeaderInformation
        {
            Round = new Round
            {
                RoundNumber = 2,
                RealTimeMinersInformation =
                {
                    { Accounts[0].KeyPair.PublicKey.ToHex(), new MinerInRound() },
                    { Accounts[1].KeyPair.PublicKey.ToHex(), new MinerInRound() },
                    { Accounts[2].KeyPair.PublicKey.ToHex(), new MinerInRound() }
                }
            }
        };

        await ParliamentStubs.First().Initialize.SendAsync(new InitializeInput
        {
            ProposerAuthorityRequired = false,
            PrivilegedProposer = Address.FromPublicKey(MissionedECKeyPairs.InitialKeyPairs.First().PublicKey)
        });
        await CreateAndIssueToken("ELF");
        await CreateAndIssueToken("READ");
        await TokenStub.Transfer.SendAsync(new TransferInput
        {
            Symbol = "READ",
            Amount = 10_00000000,
            To = ContractAddresses[ConsensusSmartContractAddressNameProvider.Name]
        });

        await mockedCrossChainStub.UpdateInformationFromCrossChain.SendAsync(new BytesValue
        {
            Value = headerInformation.ToByteString()
        });

        var minerList = await ConsensusStub.GetMainChainCurrentMinerList.CallAsync(new Empty());
        minerList.Pubkeys.Select(m => m.ToHex()).ShouldBe(headerInformation.Round.RealTimeMinersInformation.Keys);

        var balance = await TokenStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = Address.FromPublicKey(MissionedECKeyPairs.InitialKeyPairs.Skip(1).First().PublicKey),
            Symbol = "READ"
        });
        balance.Balance.ShouldBe(2_00000000);
    }

    private async Task CreateAndIssueToken(string symbol)
    {
        var defaultOrganizationAddress =
            await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
        await ParliamentReachAnAgreementAsync(new CreateProposalInput
        {
            ToAddress = ContractAddresses[TokenSmartContractAddressNameProvider.Name],
            ContractMethodName = nameof(TokenStub.Create),
            Params = new CreateInput
            {
                Symbol = symbol,
                Decimals = 8,
                TokenName = "Test",
                Issuer = Accounts[0].Address,
                IsBurnable = true,
                TotalSupply = 1_000_000_000_00000000
            }.ToByteString(),
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            OrganizationAddress = defaultOrganizationAddress
        });
        
        const long issueTokenAmount = 10_0000_00000000;
        var issueToAddress = Address.FromPublicKey(MissionedECKeyPairs.InitialKeyPairs.First().PublicKey);
        var issueTokenTransaction = TokenStub.Issue.GetTransaction(new IssueInput
        {
            Symbol = symbol,
            Amount = issueTokenAmount,
            To = issueToAddress
        });
        await BlockMiningService.MineBlockAsync(new List<Transaction>
        {
            // createTokenTransaction,
            issueTokenTransaction
        });
    }

    [Fact]
    public async Task UpdateInformationFromCrossChainTest_LowRoundNumber()
    {
        SetToSideChain();
        InitialContracts();
        var mockedCrossChain = SampleAccount.Accounts.Last();
        var mockedCrossChainStub =
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                mockedCrossChain.KeyPair);

        var headerInformation = new AElfConsensusHeaderInformation
        {
            Round = new Round
            {
                RoundNumber = 0,
                RealTimeMinersInformation =
                {
                    { Accounts[0].KeyPair.PublicKey.ToHex(), new MinerInRound() },
                    { Accounts[1].KeyPair.PublicKey.ToHex(), new MinerInRound() },
                    { Accounts[2].KeyPair.PublicKey.ToHex(), new MinerInRound() }
                }
            }
        };

        await mockedCrossChainStub.UpdateInformationFromCrossChain.SendAsync(new BytesValue
        {
            Value = headerInformation.ToByteString()
        });

        var minerList = await ConsensusStub.GetMainChainCurrentMinerList.CallAsync(new Empty());
        minerList.Pubkeys.Select(m => m.ToHex())
            .ShouldNotBe(headerInformation.Round.RealTimeMinersInformation.Keys);
    }
}