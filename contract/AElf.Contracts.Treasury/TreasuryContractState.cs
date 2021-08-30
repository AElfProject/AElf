﻿using AElf.Standards.ACS1;
using AElf.Standards.ACS10;
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

        /// <summary>
        /// -> Welcome Reward
        /// </summary>
        public SingletonState<Hash> VotesWeightRewardHash { get; set; }

        /// <summary>
        /// -> Flexible Reward
        /// </summary>
        public SingletonState<Hash> ReElectionRewardHash { get; set; }

        public MappedState<string, MethodFees> TransactionFees { get; set; }

        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }
        public SingletonState<AuthorityInfo> TreasuryController { get; set; }
        public SingletonState<SymbolList> SymbolList { get; set; }

        public SingletonState<DividendPoolWeightSetting> DividendPoolWeightSetting { get; set; }
        public SingletonState<MinerRewardWeightSetting> MinerRewardWeightSetting { get; set; }

        public MappedState<long, Dividends> DonatedDividends { get; set; }

        public SingletonState<long> MiningReward { get; set; }

        /// <summary>
        /// Pubkey -> Latest Mined Term Number.
        /// </summary>
        public MappedState<string, long> LatestMinedTerm { get; set; }

        public MappedState<string, bool> IsReplacedEvilMiner { get; set; }
        public MappedState<long, StringList> ReplaceCandidateMap { get; set; }

        public MappedState<long, bool> HasNewMiner { get; set; }
    }
}