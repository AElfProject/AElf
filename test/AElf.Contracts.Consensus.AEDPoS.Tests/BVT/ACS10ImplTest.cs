using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS10;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class AEDPoSTest
{
    [Fact]
    public async Task Consensus_Donate_With_Invalid_Token_Test()
    {
        var tokenSymbol = "SEP";

        await CreateTokenAsync(tokenSymbol);

        // Donate with token which is not profitable will fail
        var issueAmount = 1000_000;
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Amount = issueAmount,
            Symbol = tokenSymbol,
            To = BootMinerAddress
        });
        var balanceBeforeDonate = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = BootMinerAddress,
            Symbol = tokenSymbol
        });
        balanceBeforeDonate.Balance.ShouldBe(issueAmount);
        var donateRet = await AEDPoSContractStub.Donate.SendAsync(new DonateInput
        {
            Symbol = tokenSymbol,
            Amount = 1000
        });
        donateRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var balanceAfterDonate = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Owner = BootMinerAddress,
            Symbol = tokenSymbol
        });
        balanceAfterDonate.Balance.ShouldBe(issueAmount);
    }

    private async Task CreateTokenAsync(string symbol)
    {
        var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());

        const string proposalCreationMethodName = nameof(TokenContractStub.Create);
        var proposalId = await CreateProposalAsync(TokenContractAddress, defaultOrganization,
            proposalCreationMethodName, new CreateInput
            {
                Symbol = symbol,
                TokenName = "name",
                TotalSupply = 1000_000_000,
                Issuer = BootMinerAddress,
                Owner = BootMinerAddress
            });
        await ApproveWithMinersAsync(proposalId);
        var result = await ParliamentContractStub.Release.SendAsync(proposalId);
        var t = "t";
    }
}