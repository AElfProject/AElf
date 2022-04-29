using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractState : ContractState
    {
        /// <summary>
        /// Elector Pubkey -> ElectorVote
        /// </summary>
        public MappedState<string, ElectorVote> ElectorVotes { get; set; }

        /// <summary>
        /// Candidate Pubkey -> CandidateVote
        /// </summary>
        public MappedState<string, CandidateVote> CandidateVotes { get; set; }

        /// <summary>
        /// Candidate Pubkey -> CandidateInformation
        /// </summary>
        public MappedState<string, CandidateInformation> CandidateInformationMap { get; set; }

        public Int64State CurrentTermNumber { get; set; }

        /// <summary>
        /// Current Candidates.
        /// </summary>
        public SingletonState<PubkeyList> Candidates { get; set; }

        /// <summary>
        /// Candidates Ranking List.
        /// </summary>
        public SingletonState<DataCenterRankingList> DataCentersRankingList { get; set; }

        /// <summary>
        /// Pubkey -> Is Banned from Election.
        /// </summary>
        public MappedState<string, bool> BannedPubkeyMap { get; set; }

        /// <summary>
        /// Vote Id -> Lock Time (seconds)
        /// </summary>
        public MappedState<Hash, long> LockTimeMap { get; set; }

        /// <summary>
        /// Term Number -> TermSnapshot
        /// </summary>
        public MappedState<long, TermSnapshot> Snapshots { get; set; }

        /// <summary>
        /// Current Maximum Miners Count.
        /// </summary>
        public Int32State MinersCount { get; set; }

        public Int64State TimeEachTerm { get; set; }

        public SingletonState<VoteWeightInterestList> VoteWeightInterestList { get; set; }
        public SingletonState<VoteWeightProportion> VoteWeightProportion { get; set; }
        public SingletonState<AuthorityInfo> VoteWeightInterestController { get; set; }

        /// <summary>
        /// Pubkey -> Address who has the authority to replace it.
        /// </summary>
        public MappedState<string, Address> CandidateAdmins { get; set; }

        /// <summary>
        /// Pubkey -> Newest pubkey
        /// </summary>
        public MappedState<string, string> CandidateReplacementMap { get; set; }

        /// <summary>
        /// Pubkey -> Initial pubkey (First round initial miner pubkey or first announce election pubkey)
        /// </summary>
        public MappedState<string, string> InitialPubkeyMap { get; set; }

        /// <summary>
        /// Initial pubkey -> Newest pubkey
        /// </summary>
        public MappedState<string, string> InitialToNewestPubkeyMap { get; set; }

        /// <summary>
        /// Address -> Pubkey.
        /// </summary>
        public MappedState<Address, string> PubkeyMap { get; set; }

        public BoolState FixWelfareProfitDisabled { get; set; }
    }
}