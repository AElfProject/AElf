using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestKit;
using AElf.CSharp.Core;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit.BVT
{
    public partial class ProfitContractTests : ProfitContractTestBase
    {
        protected Hash schemeId { get; set; }

        public ProfitContractTests()
        {
            InitializeContracts();
        }
        [Fact]
        public async Task ProfitContract_CreateScheme_With_Invalid_Input_Test()
        {
            var creator = Creators[0];

            var createSchemeRet = await creator.CreateScheme.SendWithExceptionAsync(new CreateSchemeInput
            {
                ProfitReceivingDuePeriodCount = ProfitContractTestConstants.MaximumProfitReceivingDuePeriodCount + 1,
            });
            createSchemeRet.TransactionResult.Error.ShouldContain("Invalid profit receiving due period count");
            
            createSchemeRet = await creator.CreateScheme.SendWithExceptionAsync(new CreateSchemeInput
            {
                ProfitReceivingDuePeriodCount = -1,
            });
            createSchemeRet.TransactionResult.Error.ShouldContain("Invalid profit receiving due period count");
        }

        [Fact]
        public async Task ProfitContract_CreateScheme_Test()
        {
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorKeyPair[0].PublicKey);

            await creator.CreateScheme.SendAsync(new CreateSchemeInput
            {
                ProfitReceivingDuePeriodCount = 100,
            });

            var createdSchemeIds = (await creator.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
            {
                Manager = creatorAddress
            })).SchemeIds;

            createdSchemeIds.Count.ShouldBe(1);

            schemeId = createdSchemeIds.First();

            const long contributeAmount = (long) (ProfitContractTestConstants.NativeTokenTotalSupply * 0.1);

            await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                SchemeId = schemeId,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Amount = contributeAmount
            });

            var scheme = await ProfitContractStub.GetScheme.CallAsync(schemeId);

            scheme.Manager.ShouldBe(Address.FromPublicKey(CreatorKeyPair[0].PublicKey));

            var schemeAddress = await ProfitContractStub.GetSchemeAddress.CallAsync(
                new SchemePeriod
                {
                    SchemeId = schemeId
                });
            var schemeBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                Owner = schemeAddress
            })).Balance;

            schemeBalance.ShouldBe(contributeAmount);
        }
        
        [Fact]
        public async Task ProfitContract_DistributeProfits_Burned_Profit_Test()
        {
            const int delayDistributePeriodCount = 3;
            const int contributeAmountEachTime = 100_000;
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorKeyPair[0].PublicKey);

            await creator.CreateScheme.SendAsync(new CreateSchemeInput
            {
                IsReleaseAllBalanceEveryTimeByDefault = true,
                ProfitReceivingDuePeriodCount = 100,
                DelayDistributePeriodCount = delayDistributePeriodCount
            });

            var createdSchemeIds = (await creator.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
            {
                Manager = creatorAddress
            })).SchemeIds;

            schemeId = createdSchemeIds.First();
            var period = 1;
            var beforeBurnToken = (await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Supply;
            await ContributeAndDistribute(creator, contributeAmountEachTime, period);
            var afterBurnToken = (await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            })).Supply;
            beforeBurnToken.Sub(afterBurnToken).ShouldBe(contributeAmountEachTime);
        }

        [Fact]
        public async Task ProfitContract_DelayDistribution_Test()
        {
            const int delayDistributePeriodCount = 3;
            const int contributeAmountEachTime = 100_000;
            var creator = Creators[0];
            var creatorAddress = Address.FromPublicKey(CreatorKeyPair[0].PublicKey);

            await creator.CreateScheme.SendAsync(new CreateSchemeInput
            {
                IsReleaseAllBalanceEveryTimeByDefault = true,
                ProfitReceivingDuePeriodCount = 100,
                DelayDistributePeriodCount = delayDistributePeriodCount
            });

            var createdSchemeIds = (await creator.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
            {
                Manager = creatorAddress
            })).SchemeIds;

            schemeId = createdSchemeIds.First();

            var periodToTotalShares = new Dictionary<long, long>();
            var currentShares = 0L;

            // Distribute 3 times.
            for (var period = 1; period <= 3; period++)
            {
                currentShares += await AddBeneficiaries(creator);
                periodToTotalShares.Add(period, currentShares);

                await ContributeAndDistribute(creator, contributeAmountEachTime, period);

                // Check distributed information.
                var distributedInformation = await creator.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    Period = period,
                    SchemeId = schemeId
                });
                distributedInformation.IsReleased.ShouldBeTrue();
                distributedInformation.TotalShares.ShouldBe(0);
                distributedInformation.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(-contributeAmountEachTime);
            }

            // Distribution of period 4 will use the total shares of period 1.
            {
                await ContributeAndDistribute(creator, contributeAmountEachTime, 4);
                // Check distributed information.
                var distributedInformation = await creator.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    Period = 4,
                    SchemeId = schemeId
                });
                distributedInformation.IsReleased.ShouldBeTrue();
                distributedInformation.TotalShares.ShouldBe(periodToTotalShares[1]);
                distributedInformation.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(contributeAmountEachTime);
            }

            // Distribution of period 5 will use the total shares of period 2.
            {
                await ContributeAndDistribute(creator, contributeAmountEachTime, 5);
                // Check distributed information.
                var distributedInformation = await creator.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    Period = 5,
                    SchemeId = schemeId
                });
                distributedInformation.TotalShares.ShouldBe(periodToTotalShares[2]);
                distributedInformation.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(contributeAmountEachTime);
            }

            // Distribution of period 6 will use the total shares of period 3.
            {
                await ContributeAndDistribute(creator, contributeAmountEachTime, 6);
                // Check distributed information.
                var distributedInformation = await creator.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    Period = 6,
                    SchemeId = schemeId
                });
                distributedInformation.TotalShares.ShouldBe(periodToTotalShares[3]);
                distributedInformation.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(contributeAmountEachTime);
            }

            // Distribution of period 7 will use the total shares of period 4 (same with period 3).
            {
                await ContributeAndDistribute(creator, contributeAmountEachTime, 7);
                // Check distributed information.
                var distributedInformation = await creator.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    Period = 7,
                    SchemeId = schemeId
                });
                distributedInformation.TotalShares.ShouldBe(periodToTotalShares[3]);
                distributedInformation.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(contributeAmountEachTime);
            }
        }

        private async Task ContributeAndDistribute(ProfitContractImplContainer.ProfitContractImplStub creator,
            int contributeAmountEachTime, int period)
        {
            await creator.ContributeProfits.SendAsync(new ContributeProfitsInput
            {
                Amount = contributeAmountEachTime,
                Symbol = ProfitContractTestConstants.NativeTokenSymbol,
                SchemeId = schemeId
            });

            await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
            {
                SchemeId = schemeId,
                Period = period,
            });
        }

        private async Task<long> AddBeneficiaries(ProfitContractImplContainer.ProfitContractImplStub creator)
        {
            await creator.AddBeneficiaries.SendAsync(new AddBeneficiariesInput
            {
                SchemeId = schemeId,
                BeneficiaryShares =
                {
                    new BeneficiaryShare
                    {
                        Beneficiary = Accounts[11].Address,
                        Shares = 1
                    },
                    new BeneficiaryShare
                    {
                        Beneficiary = Accounts[12].Address,
                        Shares = 1
                    },
                    new BeneficiaryShare
                    {
                        Beneficiary = Accounts[13].Address,
                        Shares = 1
                    },
                    new BeneficiaryShare
                    {
                        Beneficiary = Accounts[14].Address,
                        Shares = 1
                    },
                    new BeneficiaryShare
                    {
                        Beneficiary = Accounts[15].Address,
                        Shares = 1
                    },
                }
            });

            return 5;
        }
    }
}