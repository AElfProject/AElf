namespace AElf.Contracts.Election
{
    public class ElectionContractConstants
    {
        public const long LockTokenForElection = 100_000;

        public const string VoteSymbol = "VOTE";
        public const long VotesTotalSupply = 200_000_000;

        public const long ElfTokenPerBlock = 100;

        public const int CitizenWelfareWeight = 20;
        public const int BackupSubsidyWeight = 20;
        public const int MinerRewardWeight = 60;
        
        public const int BasicMinerRewardWeight = 4;
        public const int VotesWeightRewardWeight = 1;
        public const int ReElectionRewardWeight = 1;

    }
}