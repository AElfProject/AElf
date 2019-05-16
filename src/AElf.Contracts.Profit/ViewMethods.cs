using System.Linq;
using AElf.Kernel;
using AElf.Types;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract
    {
        public override CreatedProfitItems GetCreatedProfitItems(GetCreatedProfitItemsInput input)
        {
            return State.CreatedProfitItemsMap[input.Creator];
        }

        public override ProfitItem GetProfitItem(Hash input)
        {
            return State.ProfitItemsMap[input];
        }

        public override Address GetProfitItemVirtualAddress(GetProfitItemVirtualAddressInput input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);
            return input.Period == 0
                ? virtualAddress
                : GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
        }

        public override ReleasedProfitsInformation GetReleasedProfitsInformation(
            GetReleasedProfitsInformationInput input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(input.ProfitId);
            var releasedProfitsVirtualAddress = GetReleasedPeriodProfitsVirtualAddress(virtualAddress, input.Period);
            return State.ReleasedProfitsMap[releasedProfitsVirtualAddress] ?? new ReleasedProfitsInformation
                       {ProfitsAmount = -1, TotalWeight = -1};
        }

        public override ProfitDetails GetProfitDetails(GetProfitDetailsInput input)
        {
            return State.ProfitDetailsMap[input.ProfitId][input.Receiver];
        }
        
        private Address GetReleasedPeriodProfitsVirtualAddress(Address profitId, long period)
        {
            return Address.FromPublicKey(period.ToString().CalculateHash().Concat(profitId.Value).ToArray());
        }
    }
}