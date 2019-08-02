using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Sdk.CSharp;
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
            scheme.UndistributedProfits[ProfitContractTestConstants.NativeTokenSymbol]
                .ShouldBe(contributeAmount);

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
                distributedInformation.ProfitsAmount[ProfitContractTestConstants.NativeTokenSymbol]
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
                distributedInformation.ProfitsAmount[ProfitContractTestConstants.NativeTokenSymbol]
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
                distributedInformation.ProfitsAmount[ProfitContractTestConstants.NativeTokenSymbol]
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
                distributedInformation.ProfitsAmount[ProfitContractTestConstants.NativeTokenSymbol]
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
                distributedInformation.ProfitsAmount[ProfitContractTestConstants.NativeTokenSymbol]
                    .ShouldBe(contributeAmountEachTime);
            }
        }

        private async Task ContributeAndDistribute(ProfitContractContainer.ProfitContractStub creator,
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
                Symbol = ProfitContractTestConstants.NativeTokenSymbol
            });
        }

        private async Task<long> AddBeneficiaries(ProfitContractContainer.ProfitContractStub creator)
        {
            await creator.AddBeneficiaries.SendAsync(new AddBeneficiariesInput
            {
                SchemeId = schemeId,
                BeneficiaryShares =
                {
                    new BeneficiaryShare
                    {
                        Beneficiary = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[11].PublicKey),
                        Shares = 1
                    },
                    new BeneficiaryShare
                    {
                        Beneficiary = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[12].PublicKey),
                        Shares = 1
                    },
                    new BeneficiaryShare
                    {
                        Beneficiary = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[13].PublicKey),
                        Shares = 1
                    },
                    new BeneficiaryShare
                    {
                        Beneficiary = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[14].PublicKey),
                        Shares = 1
                    },
                    new BeneficiaryShare
                    {
                        Beneficiary = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[15].PublicKey),
                        Shares = 1
                    },
                }
            });

            return 5;
        }
    }
}