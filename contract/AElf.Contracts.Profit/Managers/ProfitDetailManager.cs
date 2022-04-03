using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit.Managers
{
    public class ProfitDetailManager
    {
        private readonly MappedState<Hash, Address, ProfitDetails> _profitDetailsMap;

        public ProfitDetailManager(MappedState<Hash, Address, ProfitDetails> profitDetailsMap)
        {
            _profitDetailsMap = profitDetailsMap;
        }
    }
}