using Acs1;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContractState
    {
        internal MappedState<string, MethodFees> MethodFees { get; set; }

        internal MethodFeeProviderContractContainer.MethodFeeProviderContractReferenceState MethodFeeProviderContract
        {
            get;
            set;
        }

        public Int32State TransactionFeeUnitPrice { get; set; }
    }
}