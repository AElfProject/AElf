using AElf.Sdk.CSharp.State;
using AElf.Standards.ACS1;
using AElf.Types;

namespace AElf.Contracts.Election;

public partial class ElectionContractState : ContractState
{
    public BoolState Initialized { get; set; }
    public BoolState VotingEventRegistered { get; set; }

    public SingletonState<Hash> TreasuryHash { get; set; }
    public SingletonState<Hash> WelfareHash { get; set; }
    public SingletonState<Hash> SubsidyHash { get; set; }
    public SingletonState<Hash> FlexibleHash { get; set; }
    public SingletonState<Hash> WelcomeHash { get; set; }

    // Old:Pubkey/New:Address -> ElectorVote
    public MappedState<string, ElectorVote> ElectorVotes { get; set; }

    public MappedState<string, CandidateVote> CandidateVotes { get; set; }

    public MappedState<string, CandidateInformation> CandidateInformationMap { get; set; }

    public Int64State CurrentTermNumber { get; set; }

    public SingletonState<PubkeyList> Candidates { get; set; }

    public SingletonState<DataCenterRankingList> DataCentersRankingList { get; set; }

    public SingletonState<PubkeyList> InitialMiners { get; set; }

    public MappedState<string, bool> BannedPubkeyMap { get; set; }

    /// <summary>
    ///     Vote Id -> Lock Time (seconds)
    /// </summary>
    public MappedState<Hash, long> LockTimeMap { get; set; }

    public MappedState<long, TermSnapshot> Snapshots { get; set; }

    public Int32State MinersCount { get; set; }

    /// <summary>
    ///     Time unit: seconds
    /// </summary>
    public Int64State MinimumLockTime { get; set; }

    /// <summary>
    ///     Time unit: seconds
    /// </summary>
    public Int64State MaximumLockTime { get; set; }

    public Int64State TimeEachTerm { get; set; }

    public SingletonState<Hash> MinerElectionVotingItemId { get; set; }

    public MappedState<string, MethodFees> TransactionFees { get; set; }
    public SingletonState<VoteWeightInterestList> VoteWeightInterestList { get; set; }
    public SingletonState<VoteWeightProportion> VoteWeightProportion { get; set; }
    public SingletonState<AuthorityInfo> VoteWeightInterestController { get; set; }

    public SingletonState<AuthorityInfo> MethodFeeController { get; set; }

    /// <summary>
    ///     Pubkey -> Address who has the authority to replace it.
    /// </summary>
    public MappedState<string, Address> CandidateAdmins { get; set; }

    /// <summary>
    ///     Admin address -> Pubkey
    /// </summary>
    public MappedState<Address, PubkeyList> ManagedCandidatePubkeysMap { get; set; }

    /// <summary>
    ///     Pubkey -> Newest pubkey
    /// </summary>
    public MappedState<string, string> CandidateReplacementMap { get; set; }

    /// <summary>
    ///     Pubkey -> Initial pubkey (First round initial miner pubkey or first announce election pubkey)
    /// </summary>
    public MappedState<string, string> InitialPubkeyMap { get; set; }

    /// <summary>
    ///     Initial pubkey -> Newest pubkey
    /// </summary>
    public MappedState<string, string> InitialToNewestPubkeyMap { get; set; }

    public SingletonState<Address> EmergencyResponseOrganizationAddress { get; set; }

    /// <summary>
    ///     Pubkey -> Sponsor address (who will pay announce election fee for this pubkey)
    /// </summary>
    public MappedState<string, Address> CandidateSponsorMap { get; set; }

    public BoolState ElectionEnabled { get; set; }

    public MappedState<Hash, bool> WeightsAlreadyFixedMap { get; set; }
    
}