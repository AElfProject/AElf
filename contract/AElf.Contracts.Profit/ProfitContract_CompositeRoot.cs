using AElf.Contracts.Profit.Managers;
using AElf.Contracts.Profit.Services;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract
    {
        private IProfitSchemeManager GetProfitSchemeManager()
        {
            return new ProfitSchemeManager(Context, State.SchemeInfos, State.ManagingSchemeIds);
        }

        private IBeneficiaryManager GetBeneficiaryManager(IProfitSchemeManager profitSchemeManager = null,
            IProfitDetailManager profitDetailManager = null)
        {
            if (profitSchemeManager == null)
            {
                profitSchemeManager = GetProfitSchemeManager();
            }

            if (profitDetailManager == null)
            {
                profitDetailManager = GetProfitDetailManager();
            }

            return new BeneficiaryManager(Context, profitSchemeManager, profitDetailManager);
        }

        private IProfitDetailManager GetProfitDetailManager()
        {
            return new ProfitDetailManager(Context, State.ProfitDetailsMap);
        }

        private IDistributedProfitsInfoManager GetDistributedProfitsInfoManager()
        {
            return new DistributedProfitsInfoManager(Context, State.DistributedProfitsMap);
        }

        private IProfitService GetProfitService()
        {
            var profitSchemeManager = GetProfitSchemeManager();
            var profitDetailManager = GetProfitDetailManager();
            var beneficiaryManager = GetBeneficiaryManager(profitSchemeManager, profitDetailManager);
            var distributedProfitsInfoManager = GetDistributedProfitsInfoManager();
            return new ProfitService(Context, State.TokenContract, beneficiaryManager, profitDetailManager,
                profitSchemeManager, distributedProfitsInfoManager);
        }
    }
}