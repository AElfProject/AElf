using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.TransactionFees
{
    public class TransactionFeesContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public MappedState<string, TokenAmounts> MethodFees { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract.ContractContainer.ContractReferenceState Acs8Contract
        {
            get;
            set;
        }
    }
}