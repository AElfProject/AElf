using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Profit;

public partial class ProfitContractTests : ProfitContractTestBase
{
    private Hash _schemeId;

    [Fact(DisplayName = "[Profit Contract] Create a profit scheme.")]
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

        _schemeId = createdSchemeIds.First();

        const long contributeAmount = (long)(ProfitContractTestConstants.NativeTokenTotalSupply * 0.1);

        await ProfitContractStub.ContributeProfits.SendAsync(new ContributeProfitsInput
        {
            SchemeId = _schemeId,
            Symbol = ProfitContractTestConstants.NativeTokenSymbol,
            Amount = contributeAmount
        });

        var scheme = await ProfitContractStub.GetScheme.CallAsync(_schemeId);

        scheme.Manager.ShouldBe(Address.FromPublicKey(CreatorKeyPair[0].PublicKey));

        var schemeAddress = await ProfitContractStub.GetSchemeAddress.CallAsync(
            new SchemePeriod
            {
                SchemeId = _schemeId
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

        _schemeId = createdSchemeIds.First();
        var period = 1;
        var supplyBeforeBurning = (await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = ProfitContractTestConstants.NativeTokenSymbol
        })).Supply;
        await ContributeAndDistribute(creator, contributeAmountEachTime, period);
        var supplyAfterBurning = (await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
        {
            Symbol = ProfitContractTestConstants.NativeTokenSymbol
        })).Supply;
        supplyBeforeBurning.Sub(supplyAfterBurning).ShouldBe(contributeAmountEachTime);
    }


    [Fact]
    public async Task ProfitContract_AddSubScheme_Success_Test()
    {
        const int shares1 = 80;
        const int shares2 = 20;

        var creator = Creators[0];

        var schemeId = await CreateSchemeAsync();
        var subSchemeId1 = await CreateSchemeAsync(1);
        var subSchemeId2 = await CreateSchemeAsync(2);

        var subProfitItem1 = await creator.GetScheme.CallAsync(subSchemeId1);
        var subProfitItem2 = await creator.GetScheme.CallAsync(subSchemeId2);

        await creator.AddSubScheme.SendAsync(new AddSubSchemeInput
        {
            SchemeId = schemeId,
            SubSchemeId = subSchemeId1,
            SubSchemeShares = shares1
        });

        var profitDetails1 = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
        {
            SchemeId = schemeId,
            Beneficiary = subProfitItem1.VirtualAddress
        });

        // Check the total_weight of profit scheme.
        {
            var profitItem = await creator.GetScheme.CallAsync(schemeId);
            profitItem.TotalShares.ShouldBe(shares1);
        }

        profitDetails1.Details.Count.ShouldBe(1);
        profitDetails1.Details.First().StartPeriod.ShouldBe(1);
        profitDetails1.Details.First().EndPeriod.ShouldBe(long.MaxValue);
        profitDetails1.Details.First().LastProfitPeriod.ShouldBe(0);
        profitDetails1.Details.First().Shares.ShouldBe(shares1);

        await creator.AddSubScheme.SendAsync(new AddSubSchemeInput()
        {
            SchemeId = schemeId,
            SubSchemeId = subSchemeId2,
            SubSchemeShares = shares2
        });

        var profitDetails2 = await creator.GetProfitDetails.CallAsync(new GetProfitDetailsInput
        {
            SchemeId = schemeId,
            Beneficiary = subProfitItem2.VirtualAddress
        });

        // Check the total_weight of profit scheme.
        {
            var profitItem = await creator.GetScheme.CallAsync(schemeId);
            profitItem.TotalShares.ShouldBe(shares1 + shares2);
        }

        profitDetails2.Details.Count.ShouldBe(1);
        profitDetails2.Details.First().StartPeriod.ShouldBe(1);
        profitDetails2.Details.First().EndPeriod.ShouldBe(long.MaxValue);
        profitDetails2.Details.First().LastProfitPeriod.ShouldBe(0);
        profitDetails2.Details.First().Shares.ShouldBe(shares2);
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
            DelayDistributePeriodCount = delayDistributePeriodCount,
            CanRemoveBeneficiaryDirectly = true
        });

        var createdSchemeIds = (await creator.GetManagingSchemeIds.CallAsync(new GetManagingSchemeIdsInput
        {
            Manager = creatorAddress
        })).SchemeIds;

        _schemeId = createdSchemeIds.First();

        var periodToTotalShares = new Dictionary<long, long>();
        var currentShares = 0L;

        // Distribute 3 times.
        for (var period = 1; period <= 3; period++)
        {
            currentShares += await AddBeneficiariesAsync(creator);
            periodToTotalShares.Add(period, currentShares);

            await ContributeAndDistribute(creator, contributeAmountEachTime, period);

            // Check distributed information.
            var distributedInformation = await creator.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
            {
                Period = period,
                SchemeId = _schemeId
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
                SchemeId = _schemeId
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
                SchemeId = _schemeId
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
                SchemeId = _schemeId
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
                SchemeId = _schemeId
            });
            distributedInformation.TotalShares.ShouldBe(periodToTotalShares[3]);
            distributedInformation.AmountsMap[ProfitContractTestConstants.NativeTokenSymbol]
                .ShouldBe(contributeAmountEachTime);
        }

        {
            await ContributeAndDistribute(creator, contributeAmountEachTime, 8);
            await RemoveBeneficiaryAsync(creator, Accounts[11].Address);
            var scheme = await creator.GetScheme.CallAsync(_schemeId);
            scheme.CachedDelayTotalShares.Values.ShouldAllBe(v => v == 12);
            scheme.TotalShares.ShouldBe(12);
        }
        
        {
            await ContributeAndDistribute(creator, contributeAmountEachTime, 9);
            // Check distributed information.
            var distributedInformation = await creator.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
            {
                Period = 9,
                SchemeId = _schemeId
            });
            distributedInformation.TotalShares.ShouldBe(12);
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
            SchemeId = _schemeId
        });

        await creator.DistributeProfits.SendAsync(new DistributeProfitsInput
        {
            SchemeId = _schemeId,
            Period = period,
        });
    }

    private async Task<long> AddBeneficiariesAsync(ProfitContractImplContainer.ProfitContractImplStub creator)
    {
        await creator.AddBeneficiaries.SendAsync(new AddBeneficiariesInput
        {
            SchemeId = _schemeId,
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

    private async Task RemoveBeneficiaryAsync(ProfitContractImplContainer.ProfitContractImplStub creator,
        Address beneficiary)
    {
        await creator.RemoveBeneficiary.SendAsync(new RemoveBeneficiaryInput
        {
            Beneficiary = beneficiary,
            SchemeId = _schemeId
        });
    }
}