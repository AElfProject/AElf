using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using Shouldly;
using Xunit;

// ReSharper disable HeuristicUnreachableCode
namespace AElf.Contracts.TokenHolder
{
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
        public async Task DistributeProfits_ClaimWithProfitContract()
        {
            await AddBeneficiaryTest();

            var tokenHolderProfitScheme = await TokenHolderContractStub.GetScheme.CallAsync(Starter);

            await TokenHolderContractStub.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeManager = Starter,
                Symbol = "ELF"
            });

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = UserAddresses.First(),
                    Symbol = "ELF"
                })).Balance;
                balance.ShouldBe((long) (TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1));
            }

            var userProfitStub =
                GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, UserKeyPairs.First());
            await userProfitStub.ClaimProfits.SendAsync(new Profit.ClaimProfitsInput
            {
                SchemeId = tokenHolderProfitScheme.SchemeId,
                Symbol = "ELF"
            });
            
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = UserAddresses.First(),
                    Symbol = "ELF"
                })).Balance;
                balance.ShouldBe((long) (TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1) + 10000);
            }
        }
        
        [Fact]
        public async Task DistributeProfits_ClaimWithTokenHolderContract()
        {
            await AddBeneficiaryTest();

            await TokenHolderContractStub.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeManager = Starter,
                Symbol = "ELF"
            });

            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = UserAddresses.First(),
                    Symbol = "ELF"
                })).Balance;
                balance.ShouldBe((long) (TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1));
            }

            var userTokenHolderStub =
                GetTester<TokenHolderContractContainer.TokenHolderContractStub>(TokenHolderContractAddress, UserKeyPairs.First());
            await userTokenHolderStub.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeManager = Starter,
                Symbol = "ELF"
            });
            
            {
                var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = UserAddresses.First(),
                    Symbol = "ELF"
                })).Balance;
                balance.ShouldBe((long) (TokenHolderContractTestConstants.NativeTokenTotalSupply * 0.1) + 10000);
            }
        }
    }
}