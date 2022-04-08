using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractState
    {
        public MappedState<string, MethodFees> TransactionFees { get; set; }

        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}