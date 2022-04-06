using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContract
    {
        private Hash GenerateSchemeId(CreateSchemeInput createSchemeInput)
        {
            var manager = createSchemeInput.Manager ?? Context.Sender;
            if (createSchemeInput.Token != null)
                return Context.GenerateId(Context.Self, createSchemeInput.Token);
            var createdSchemeCount = State.ManagingSchemeIds[manager]?.SchemeIds.Count ?? 0;
            return Context.GenerateId(Context.Self, createdSchemeCount.ToBytes(false));
        }

        private void MakeSureReferenceStateAddressSet(ContractReferenceState state, string contractSystemName)
        {
            if (state.Value != null)
                return;
            state.Value = Context.GetContractAddressByName(contractSystemName);
        }
    }
}