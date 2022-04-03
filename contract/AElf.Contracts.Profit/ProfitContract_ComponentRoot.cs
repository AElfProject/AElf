using AElf.Contracts.Profit.Managers;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract
    {
        private ProfitSchemeManager GetProfitSchemeManager()
        {
            return new ProfitSchemeManager(Context, State.SchemeInfos, State.ManagingSchemeIds);
        }
    }
}