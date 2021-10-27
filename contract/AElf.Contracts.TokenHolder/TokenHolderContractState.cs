using AElf.Standards.ACS1;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TokenHolder
{
    public partial class TokenHolderContractState : ContractState
    {
        public MappedState<Address, TokenHolderProfitScheme> TokenHolderProfitSchemes { get; set; }
        
        /// <summary>
        /// Contract address (Manager address) -> Beneficiary address -> Lock id.
        /// </summary>
        public MappedState<Address, Address, Hash> LockIds { get; set; }
        public MappedState<Hash, Timestamp> LockTimestamp { get; set; }
        
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
    }
}