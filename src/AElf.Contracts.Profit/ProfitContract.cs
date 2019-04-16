using System.Linq;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract : ProfitContractContainer.ProfitContractBase
    {
        public override Empty InitializeProfitContract(InitializeProfitContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.TokenContractSystemName.Value = input.TokenContractSystemName;

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Hash CreateProfitItem(CreateProfitItemInput input)
        {
            var profitId = GetProfitId(input.Creator, input.ItemName);
            State.ProfitsMap[profitId] = new ProfitItem
            {
                Creator = input.Creator,
                ItemName = input.ItemName,
                ProfitId = GetProfitId(input.Creator, input.ItemName),
                TotalWeight = input.TotalWeight,
                IsTotalWeightFixed = input.IsTotalWeightFixed
            };
            return profitId;
        }

        private Hash GetProfitId(Address creator, string itemName)
        {
            return Hash.FromRawBytes(creator.Value.Concat(itemName.CalculateHash()).ToArray());
        }
    }
}