using Acs1;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEPoW
{
    public partial class AEPoWContractState : ContractState
    {
        public MappedState<string, MethodFees> TransactionFees { get; set; }
        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }

        public MappedState<long, PoWRecord> Records { get; set; }

        public SingletonState<string> CurrentDifficulty { get; set; }

        public SingletonState<Timestamp> BlockchainStartTime { get; set; }

        public SingletonState<long> SupposedProduceMilliseconds { get; set; }

        public SingletonState<string> CoinBaseTokenSymbol { get; set; }
    }
}