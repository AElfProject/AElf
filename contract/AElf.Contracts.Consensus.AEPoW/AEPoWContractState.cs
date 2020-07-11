using Acs1;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContractState : ContractState
    {
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }

        public MappedState<long, string> BlockProducers { get; set; }
    }
}