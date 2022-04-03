using AElf.Contracts.Profit.Managers;

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
    }
}