using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Profit
{
    public class ProfitContract : ProfitContractContainer.ProfitContractBase
    {
        public override Empty InitializeProfitContract(InitializeProfitContractInput input)
        {
            State.TokenContractSystemName.Value = input.TokenContractSystemName;
            return new Empty();
        }
    }
}