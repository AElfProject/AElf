using Acs5;
using AElf.Contracts.Profit;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs5.Tests.TestContract
{
    public class ContractState : AElf.Sdk.CSharp.State.ContractState
    {
        public MappedState<string, MethodProfitFee> MethodProfitFees { get; set; }

        public SingletonState<Hash> ProfitId { get; set; }

        public SingletonState<long> ReleasedTimes { get; set; }
        
        internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }
    }
}