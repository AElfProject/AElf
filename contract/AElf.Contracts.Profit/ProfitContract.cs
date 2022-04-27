using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    /// <summary>
    /// Let's imagine a scenario:
    /// 1. Ean creates a profit scheme FOO: Ean calls CreateScheme. We call this profit scheme PI_FOO.
    /// 2. GL creates another profit scheme BAR: GL calls CreateScheme. We call this profit scheme PI_BAR.
    /// 3. Ean (as the creator of PI_FOO) register PI_BAR as a sub profit scheme as PI_FOO:
    /// Ean call RemoveSubScheme (SchemeId: PI_BAR's profit id, Shares : 1)
    /// 4. Anil has an account which address is ADDR_Anil.
    /// 5. Ean registers address ADDR_Anil as a profit Beneficiary of PI_FOO: Ean calls AddBeneficiary (Beneficiary: ADDR_Anil, Shares : 1)
    /// 6: Now PI_FOO is organized like this:
    ///         PI_FOO
    ///        /      \
    ///       1        1
    ///      /          \
    ///    PI_BAR     ADDR_Anil
    ///    (Total Shares is 2)
    /// 7. Ean adds some ELF tokens to PI_FOO: Ean calls DistributeProfits (Symbol: "ELF", Amount: 1000L, Period: 1)
    /// 8. Ean calls DistributeProfits: Balance of PI_BAR is 500L (PI_BAR's general ledger balance, also we can say balance of virtual address of PI_BAR is 500L),
    /// 9. Balance of PI_FOO's virtual address of first period is 500L.
    /// 10. Anil can only get his profits by calling Profit (SchemeId: PI_BAR's profit id, Symbol: "ELF")
    /// </summary>
    public partial class ProfitContract : ProfitContractImplContainer.ProfitContractImplBase
    {
        /// <summary>
        /// Create a Scheme of profit distribution.
        /// At the first time, the scheme's id is unknown,it may create by transaction id and createdSchemeIds;
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Hash CreateScheme(CreateSchemeInput input)
        {
            MakeSureReferenceStateAddressSet(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            var schemeId = GenerateSchemeId(input);
            GetProfitSchemeManager().CreateNewScheme(new Scheme
            {
                SchemeId = schemeId,
                // The address of general ledger for current profit scheme.
                VirtualAddress = Context.ConvertVirtualAddressToContractAddress(schemeId),
                Manager = input.Manager ?? Context.Sender,
                ProfitReceivingDuePeriodCount = input.ProfitReceivingDuePeriodCount,
                CurrentPeriod = 1,
                IsReleaseAllBalanceEveryTimeByDefault = input.IsReleaseAllBalanceEveryTimeByDefault,
                DelayDistributePeriodCount = input.DelayDistributePeriodCount,
                CanRemoveBeneficiaryDirectly = input.CanRemoveBeneficiaryDirectly
            });

            return schemeId;
        }

        /// <summary>
        /// Add a child to a existed scheme.
        /// </summary>
        /// <param name="input">AddSubSchemeInput</param>
        /// <returns></returns>
        public override Empty AddSubScheme(AddSubSchemeInput input)
        {
            var profitSchemeManager = GetProfitSchemeManager();
            profitSchemeManager.AddSubScheme(input.SchemeId, input.SubSchemeId, input.SubSchemeShares);
            var beneficiaryManager = GetBeneficiaryManager(profitSchemeManager);
            beneficiaryManager.AddBeneficiary(input.SchemeId, new BeneficiaryShare
            {
                Beneficiary = Context.ConvertVirtualAddressToContractAddress(input.SubSchemeId),
                Shares = input.SubSchemeShares
            }, long.MaxValue); // Profits may last forever in `AddSubScheme` case.
            return new Empty();
        }

        public override Empty RemoveSubScheme(RemoveSubSchemeInput input)
        {
            var profitSchemeManager = GetProfitSchemeManager();
            profitSchemeManager.RemoveSubScheme(input.SchemeId, input.SubSchemeId);
            var beneficiaryManager = GetBeneficiaryManager(profitSchemeManager);
            beneficiaryManager.RemoveBeneficiary(input.SchemeId,
                Context.ConvertVirtualAddressToContractAddress(input.SubSchemeId), isSubScheme: true);
            return new Empty();
        }

        public override Empty AddBeneficiary(AddBeneficiaryInput input)
        {
            GetBeneficiaryManager()
                .AddBeneficiary(input.SchemeId, input.BeneficiaryShare, input.EndPeriod, input.StartPeriod,
                    input.ProfitDetailId, input.IsFixProfitDetail);
            return new Empty();
        }

        public override Empty RemoveBeneficiary(RemoveBeneficiaryInput input)
        {
            GetBeneficiaryManager().RemoveBeneficiary(input.SchemeId, input.Beneficiary, input.ProfitDetailId);
            return new Empty();
        }

        public override Empty AddBeneficiaries(AddBeneficiariesInput input)
        {
            var beneficiaryManager = GetBeneficiaryManager();
            foreach (var beneficiaryShare in input.BeneficiaryShares)
            {
                beneficiaryManager.AddBeneficiary(input.SchemeId, beneficiaryShare, input.EndPeriod);
            }

            return new Empty();
        }

        public override Empty RemoveBeneficiaries(RemoveBeneficiariesInput input)
        {
            var beneficiaryManager = GetBeneficiaryManager();
            foreach (var beneficiary in input.Beneficiaries)
            {
                beneficiaryManager.RemoveBeneficiary(input.SchemeId, beneficiary);
            }

            return new Empty();
        }

        public override Empty FixProfitDetail(FixProfitDetailInput input)
        {
            Assert(input.SchemeId != null, "Invalid scheme id.");
            var scheme = State.SchemeInfos[input.SchemeId];

            if (Context.Sender != scheme.Manager && Context.Sender !=
                Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName))
            {
                throw new AssertionException("Only manager or token holder contract can add beneficiary.");
            }

            GetProfitService()
                .FixProfitDetail(input.SchemeId, input.BeneficiaryShare, input.StartPeriod, input.EndPeriod,
                    input.ProfitDetailId);
            return new Empty();
        }

        /// <summary>
        /// Will burn/destroy a certain amount of profits if `input.Period` is less than 0.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty DistributeProfits(DistributeProfitsInput input)
        {
            MakeSureReferenceStateAddressSet(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            GetProfitService().Distribute(input.SchemeId, input.Period,
                input.AmountsMap.ToDictionary(p => p.Key, p => p.Value));
            return new Empty();
        }

        public override Empty ContributeProfits(ContributeProfitsInput input)
        {
            MakeSureReferenceStateAddressSet(State.TokenContract, SmartContractConstants.TokenContractSystemName);
            GetProfitService().Contribute(input.SchemeId, input.Period, input.Symbol, input.Amount);
            return new Empty();
        }

        public override Empty ResetManager(ResetManagerInput input)
        {
            var profitSchemeManager = GetProfitSchemeManager();
            profitSchemeManager.ResetSchemeManager(input.SchemeId, input.NewManager);
            return new Empty();
        }

        /// <summary>
        /// Gain the profit form SchemeId from Details.lastPeriod to scheme.currentPeriod - 1;
        /// </summary>
        /// <param name="input">ClaimProfitsInput</param>
        /// <returns></returns>
        public override Empty ClaimProfits(ClaimProfitsInput input)
        {
            GetProfitService().Claim(input.SchemeId, input.Beneficiary ?? Context.Sender);
            return new Empty();
        }
    }
}