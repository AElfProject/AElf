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
            State.ProfitItemsMap[profitId] = new ProfitItem
            {
                Creator = input.Creator,
                ItemName = input.ItemName,
                ProfitId = GetProfitId(input.Creator, input.ItemName),
                TotalWeight = input.IsTotalWeightFixed ? input.TotalWeight : 0,
                IsTotalWeightFixed = input.IsTotalWeightFixed
            };
            return profitId;
        }

        public override Empty AddWeight(UpdateWeightInput input)
        {
            Assert(input.Weight >= 0, "Invalid weight.");
            var profitId = GetProfitId(Context.Sender, input.ItemName);
            var profitItem = State.ProfitItemsMap[profitId];
            Assert(profitItem != null, "Profit item not found.");
            State.WeightsMap[profitId][input.Receiver] += input.Weight;
            if (!profitItem.IsTotalWeightFixed)
            {
                profitItem.TotalWeight += input.Weight;
                State.ProfitItemsMap[profitId] = profitItem;
            }
            return new Empty();
        }

        public override Empty RemoveWeight(UpdateWeightInput input)
        {
            Assert(input.Weight >= 0, "Invalid weight.");
            var profitId = GetProfitId(Context.Sender, input.ItemName);
            var profitItem = State.ProfitItemsMap[profitId];
            Assert(profitItem != null, "Profit item not found.");
            var currentWeight = State.WeightsMap[profitId][input.Receiver];
            Assert(currentWeight >= input.Weight, "Strange weight.");
            State.WeightsMap[profitId][input.Receiver] = currentWeight - input.Weight;
            if (!profitItem.IsTotalWeightFixed)
            {
                profitItem.TotalWeight -= input.Weight;
                State.ProfitItemsMap[profitId] = profitItem;
            }
            return new Empty();
        }

        public override Empty ReleaseProfit(ReleaseProfitInput input)
        {
            return new Empty();
        }

        private Hash GetProfitId(Address creator, string itemName)
        {
            return Hash.FromRawBytes(creator.Value.Concat(itemName.CalculateHash()).ToArray());
        }
    }
}