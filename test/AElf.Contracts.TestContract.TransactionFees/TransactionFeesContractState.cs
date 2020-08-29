using AElf.Standards.ACS1;
using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.TestContract.TransactionFees
{
    public class TransactionFeesContractState : ContractState
    {
        public MappedState<int, int> TestInfo { get; set; }
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests.TestContract.ContractContainer.ContractReferenceState Acs8Contract
        {
            get;
            set;
        }
    }
}