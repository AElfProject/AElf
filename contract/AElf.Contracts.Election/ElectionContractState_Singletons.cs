using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Election;

public partial class ElectionContractState
{
    public BoolState Initialized { get; set; }
    public BoolState VotingEventRegistered { get; set; }

    public SingletonState<Hash> TreasuryHash { get; set; }
    public SingletonState<Hash> WelfareHash { get; set; }
    public SingletonState<Hash> SubsidyHash { get; set; }
    public SingletonState<Hash> FlexibleHash { get; set; }
    public SingletonState<Hash> WelcomeHash { get; set; }

    public SingletonState<PubkeyList> InitialMiners { get; set; }

    /// <summary>
    /// Time unit: seconds
    /// </summary>
    public Int64State MinimumLockTime { get; set; }

    /// <summary>
    /// Time unit: seconds
    /// </summary>
    public Int64State MaximumLockTime { get; set; }

    public SingletonState<Hash> MinerElectionVotingItemId { get; set; }

    public SingletonState<Address> EmergencyResponseOrganizationAddress { get; set; }
}