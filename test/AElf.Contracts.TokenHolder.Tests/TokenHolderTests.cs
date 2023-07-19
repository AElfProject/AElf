using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.CSharp.Core;
using AElf.Types;
using Shouldly;
using Xunit;

// ReSharper disable HeuristicUnreachableCode
namespace AElf.Contracts.TokenHolder;

public partial class TokenHolderTests : TokenHolderContractTestBase
{
    public TokenHolderTests()
    {
        InitializeContracts();
    }

    [Fact]
    public async Task CheckTokenHolderProfitScheme()
    {
        var schemeIds = await ProfitContractStub.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
        {
            Manager = DAppContractAddress
        });
        schemeIds.SchemeIds.Count.ShouldBePositive();
        var schemeId = schemeIds.SchemeIds.First();
        var scheme = await ProfitContractStub.GetScheme.CallAsync(schemeId);
        scheme.Manager.ShouldBe(DAppContractAddress);
    }

    [Fact]
    public async Task CreateTokenHolderProfitSchemeTest()
    {
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = "APP"
        });

        {
            var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);
            tokenHolderProfitScheme.Period.ShouldBe(0);
            tokenHolderProfitScheme.Symbol.ShouldBe("APP");
            tokenHolderProfitScheme.SchemeId.ShouldBeNull();
        }

        await TokenHolderContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeManager = Starter,
            Symbol = "ELF",
            Amount = 1
        });

        {
            var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);
            tokenHolderProfitScheme.SchemeId.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task ContributeProfitsTest()
    {
        await CreateTokenHolderProfitSchemeTest();

        var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

        {
            var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
            originScheme.Manager.ShouldBe(Starter);
            originScheme.CurrentPeriod.ShouldBe(1);
            originScheme.TotalShares.ShouldBe(0);

            var generalLedgerBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = originScheme.VirtualAddress,
                Symbol = "ELF"
            })).Balance;
            generalLedgerBalance.ShouldBe(1);
        }

        await TokenHolderContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeManager = Starter,
            Symbol = "ELF",
            Amount = 9999
        });

        {
            var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
            var generalLedgerBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = originScheme.VirtualAddress,
                Symbol = "ELF"
            })).Balance;
            generalLedgerBalance.ShouldBe(10000);
        }
    }

    [Fact]
    public async Task AddBeneficiaryTest()
    {
        await ContributeProfitsTest();

        var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

        await TokenHolderContractStub.AddBeneficiary.SendAsync(new AddTokenHolderBeneficiaryInput
        {
            Beneficiary = UserAddresses.First(),
            Shares = 1
        });

        {
            var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
            originScheme.TotalShares.ShouldBe(1);
        }
    }

    [Fact]
    public async Task AddBeneficiary_Repeatedly_Test()
    {
        await AddBeneficiaryTest();
        var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);
        var newShare = 2;
        await TokenHolderContractStub.AddBeneficiary.SendAsync(new AddTokenHolderBeneficiaryInput
        {
            Beneficiary = UserAddresses.First(),
            Shares = newShare
        });

        {
            var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
            originScheme.TotalShares.ShouldBe(newShare);
        }
    }

    [Fact]
    public async Task RemoveBeneficiaryTest()
    {
        await AddBeneficiaryTest();

        var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

        await TokenHolderContractStub.RemoveBeneficiary.SendAsync(new RemoveTokenHolderBeneficiaryInput
        {
            Beneficiary = UserAddresses.First()
        });

        {
            var originScheme = await ProfitContractStub.GetScheme.CallAsync(tokenHolderProfitScheme.SchemeId);
            originScheme.TotalShares.ShouldBe(0);
        }
    }

    [Fact]
    public async Task RemoveBeneficiary_With_Amount_Test()
    {
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = "ELF"
        });
        await TokenHolderContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeManager = Starter,
            Symbol = "ELF",
            Amount = 9999
        });
        await TokenHolderContractStub.AddBeneficiary.SendAsync(new AddTokenHolderBeneficiaryInput
        {
            Beneficiary = Starter,
            Shares = 1000
        });
        var schemeIds = await ProfitContractStub.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
        {
            Manager = Starter
        });
        var schemeId = schemeIds.SchemeIds[0];
        var beforeRemoveScheme = await ProfitContractStub.GetScheme.CallAsync(schemeId);
        var amount = 10;
        await TokenHolderContractStub.RemoveBeneficiary.SendAsync(new RemoveTokenHolderBeneficiaryInput
        {
            Beneficiary = Starter,
            Amount = amount
        });
        var afterRemoveScheme = await ProfitContractStub.GetScheme.CallAsync(schemeIds.SchemeIds[0]);
        afterRemoveScheme.TotalShares.ShouldBe(beforeRemoveScheme.TotalShares - amount);
        var profitAmount = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
        {
            Beneficiary = Starter,
            SchemeId = schemeId
        });
        profitAmount.Details.Count.ShouldBe(2);
        profitAmount.Details[0].Shares.ShouldBe(beforeRemoveScheme.TotalShares);
        profitAmount.Details[0].EndPeriod.ShouldBe(0);
        profitAmount.Details[1].Shares.ShouldBe(beforeRemoveScheme.TotalShares - amount);
    }

    [Fact]
    public async Task DistributeProfits_ClaimWithProfitContract()
    {
        await AddBeneficiaryTest();

        var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

        await TokenHolderContractStub.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeManager = Starter,
            AmountsMap = { { "ELF", 0L } }
        });

        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = UserAddresses.First(),
                Symbol = "ELF"
            })).Balance;
            balance.ShouldBe((long)(TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1));
        }

        var userProfitStub =
            GetTester<ProfitContractImplContainer.ProfitContractImplStub>(ProfitContractAddress, UserKeyPairs.First());
        await userProfitStub.ClaimProfits.SendAsync(new Profit.ClaimProfitsInput
        {
            SchemeId = tokenHolderProfitScheme.SchemeId
        });

        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = UserAddresses.First(),
                Symbol = "ELF"
            })).Balance;
            balance.ShouldBe((long)(TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1) + 10000);
        }
    }

    [Fact]
    public async Task DistributeProfits_ClaimWithTokenHolderContract()
    {
        await AddBeneficiaryTest();

        await TokenHolderContractStub.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeManager = Starter,
            AmountsMap = { { "ELF", 0L } }
        });

        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = UserAddresses.First(),
                Symbol = "ELF"
            })).Balance;
            balance.ShouldBe((long)(TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1));
        }

        var userTokenHolderStub =
            GetTester<TokenHolderContractImplContainer.TokenHolderContractImplStub>(TokenHolderContractAddress,
                UserKeyPairs.First());
        await userTokenHolderStub.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeManager = Starter
        });

        {
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = UserAddresses.First(),
                Symbol = "ELF"
            })).Balance;
            balance.ShouldBe((long)(TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1) + 10000);
        }
    }

    [Fact]
    public async Task AddBeneficiary_With_Invalid_Scheme()
    {
        var ret = await TokenHolderContractStub.AddBeneficiary.SendWithExceptionAsync(
            new AddTokenHolderBeneficiaryInput
            {
                Beneficiary = new Address(),
                Shares = 100
            });
        ret.TransactionResult.Error.ShouldContain("token holder profit scheme not found");
    }

    [Fact]
    public async Task DistributeProfits_Without_Authority_Test()
    {
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = "Test"
        });
        var senderWithoutAuthority =
            GetTester<TokenHolderContractImplContainer.TokenHolderContractImplStub>(TokenHolderContractAddress,
                UserKeyPairs.First());
        var distributeRet = await senderWithoutAuthority.DistributeProfits.SendWithExceptionAsync(
            new DistributeProfitsInput
            {
                SchemeManager = Starter
            });
        distributeRet.TransactionResult.Error.ShouldContain("No permission to distribute profits");
    }

    [Fact]
    public async Task RegisterForProfits_Repeatedly_Test()
    {
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = "ELF",
            AutoDistributeThreshold = { { "ELF", 1000 } }
        });
        await TokenHolderContractStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
        {
            Amount = 10,
            SchemeManager = Starter
        });
        var repeatRegisterRet = await TokenHolderContractStub.RegisterForProfits.SendWithExceptionAsync(
            new RegisterForProfitsInput
            {
                Amount = 10,
                SchemeManager = Starter
            });
        repeatRegisterRet.TransactionResult.Error.ShouldContain("Already registered.");
    }

    [Fact]
    public async Task RegisterForProfits_Without_Auto_Distribute_Test()
    {
        var amount = 10;
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = "ELF",
            AutoDistributeThreshold = { { "ELF", 1000 } }
        });
        await TokenHolderContractStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
        {
            Amount = amount,
            SchemeManager = Starter
        });
        var schemeIds = await ProfitContractStub.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
        {
            Manager = Starter
        });
        var schemeId = schemeIds.SchemeIds.First();

        var profitDetail = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
        {
            SchemeId = schemeId,
            Beneficiary = Starter
        });
        profitDetail.Details.Count.ShouldBe(1);
        profitDetail.Details[0].Shares.ShouldBe(amount);
        var schemeInfo = await ProfitContractStub.GetScheme.CallAsync(schemeId);
        schemeInfo.TotalShares.ShouldBe(amount);
    }

    [Fact]
    public async Task RegisterForProfits_With_Auto_Distribute_Test()
    {
        var amount = 1000L;
        var nativeTokenSymbol = TokenHolderContractTestConstants.NativeTokenSymbol;
        var tokenA = "JUN";
        await StarterCreateIssueAndApproveTokenAsync(tokenA, 1000000L, 100000L);
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = nativeTokenSymbol,
            AutoDistributeThreshold =
            {
                { nativeTokenSymbol, amount },
                { tokenA, amount }
            }
        });
        await TokenHolderContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeManager = Starter,
            Amount = amount,
            Symbol = nativeTokenSymbol
        });
        await TokenHolderContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeManager = Starter,
            Amount = amount,
            Symbol = tokenA
        });
        var beforeLockBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = nativeTokenSymbol,
            Owner = Starter
        })).Balance;
        await TokenHolderContractStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
        {
            Amount = amount,
            SchemeManager = Starter
        });
        var afterLockBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = nativeTokenSymbol,
            Owner = Starter
        })).Balance;
        beforeLockBalance.ShouldBe(afterLockBalance.Add(amount));
        var schemeIds = await ProfitContractStub.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
        {
            Manager = Starter
        });
        var schemeId = schemeIds.SchemeIds.First();
        var profitMap = await ProfitContractStub.GetProfitsMap.CallAsync(new Profit.ClaimProfitsInput
        {
            Beneficiary = Starter,
            SchemeId = schemeId
        });
        profitMap.Value.Count.ShouldBe(2);
        profitMap.Value.ContainsKey(nativeTokenSymbol).ShouldBeTrue();
        profitMap.Value[nativeTokenSymbol].ShouldBe(amount);
        var schemeInfoInProfit = await ProfitContractStub.GetScheme.CallAsync(schemeId);
        var schemeInfoInTokenHolder = await TokenHolderContractStub.GetScheme.CallAsync(Starter);
        schemeInfoInProfit.CurrentPeriod.ShouldBe(2);
        schemeInfoInTokenHolder.Period.ShouldBe(2);
    }

    [Fact]
    public async Task Withdraw_With_Invalid_Lock_Id_Test()
    {
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = "TEST"
        });

        var withDrawRet = await TokenHolderContractStub.Withdraw.SendWithExceptionAsync(Starter);
        withDrawRet.TransactionResult.Error.ShouldContain("Sender didn't register for profits.");
    }

    [Fact]
    public async Task Withdraw_Test()
    {
        var amount = 1000L;
        var nativeTokenSymbol = TokenHolderContractTestConstants.NativeTokenSymbol;
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = nativeTokenSymbol
        });
        await TokenHolderContractStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
        {
            Amount = amount,
            SchemeManager = Starter
        });
        var beforeUnLockBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = nativeTokenSymbol,
            Owner = Starter
        })).Balance;
        await TokenHolderContractStub.Withdraw.SendAsync(Starter);
        var afterUnLockBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = nativeTokenSymbol,
            Owner = Starter
        })).Balance;
        afterUnLockBalance.ShouldBe(beforeUnLockBalance.Add(amount));
    }

    [Fact]
    public async Task GetProfitsMap_Test()
    {
        var amount = 1000L;
        var nativeTokenSymbol = TokenHolderContractTestConstants.NativeTokenSymbol;
        var tokenA = "AUG";
        await StarterCreateIssueAndApproveTokenAsync(tokenA, 1000000L, 100000L);
        await TokenHolderContractStub.CreateScheme.SendAsync(new CreateTokenHolderProfitSchemeInput
        {
            Symbol = nativeTokenSymbol,
            AutoDistributeThreshold =
            {
                { nativeTokenSymbol, amount },
                { tokenA, amount }
            }
        });
        await TokenHolderContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeManager = Starter,
            Amount = amount,
            Symbol = nativeTokenSymbol
        });
        await TokenHolderContractStub.RegisterForProfits.SendAsync(new RegisterForProfitsInput
        {
            Amount = amount,
            SchemeManager = Starter
        });
        var profitMap = await TokenHolderContractStub.GetProfitsMap.CallAsync(new ClaimProfitsInput
        {
            Beneficiary = Starter,
            SchemeManager = Starter
        });
        profitMap.Value.Count.ShouldBe(1);
        profitMap.Value.ContainsKey(nativeTokenSymbol).ShouldBeTrue();
        profitMap.Value[nativeTokenSymbol].ShouldBe(amount);
    }


    private async Task StarterCreateIssueAndApproveTokenAsync(string symbol, long totalSupply, long issueAmount)
    {
        await TokenContractStub.Create.SendAsync(new CreateInput
        {
            Symbol = symbol,
            TokenName = symbol + " name",
            TotalSupply = totalSupply,
            Issuer = Starter,
            Owner = Starter,
            LockWhiteList = { ProfitContractAddress }
        });
        await TokenContractStub.Issue.SendAsync(new IssueInput
        {
            Amount = issueAmount,
            Symbol = symbol,
            To = Starter
        });
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Amount = issueAmount,
            Symbol = symbol,
            Spender = TokenHolderContractAddress
        });
    }
}