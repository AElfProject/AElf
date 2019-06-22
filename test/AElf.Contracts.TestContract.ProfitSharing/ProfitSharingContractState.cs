using Acs5;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.TestContract.ProfitSharing
{
    public partial class ProfitSharingContractState : ContractState
    {
        public MappedState<string, MethodProfitFee> MethodProfitFees { get; set; }

        public SingletonState<Hash> ProfitId { get; set; }

        public SingletonState<long> ReleasedTimes { get; set; }

        public SingletonState<string> TokenSymbol { get; set; }
    }
}