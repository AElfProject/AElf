using Acs1;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Treasury
{
    public partial class TreasuryContractState : ContractState
    {
        public SingletonState<Address> TreasuryVirtualAddress { get; set; }

        public SingletonState<bool> Initialized { get; set; }

        public SingletonState<Hash> TreasuryHash { get; set; }

        public SingletonState<Hash> WelfareHash { get; set; }
        public SingletonState<Hash> SubsidyHash { get; set; }
        public SingletonState<Hash> RewardHash { get; set; }

        public SingletonState<Hash> BasicRewardHash { get; set; }
        public SingletonState<Hash> VotesWeightRewardHash { get; set; }
        public SingletonState<Hash> ReElectionRewardHash { get; set; }

        public SingletonState<MinerReElectionInformation> MinerReElectionInformation { get; set; }
        
        public MappedState<string, TokenAmounts> TransactionFees { get; set; }
    }
}