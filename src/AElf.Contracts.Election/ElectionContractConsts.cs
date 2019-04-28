namespace AElf.Contracts.Election
{
    public class ElectionContractConsts
    {
        public static readonly Hash Topic = Hash.FromString("Mainchain Election");
        public const long LockTokenForElection = 100_000;
        public const string VoteSymbol = "VOTE";
        public const long VotesTotalSupply = 200_000_000;
        public const long ElfTokenPerBlock = 100;

        public const int CitizenWelfareWeight = 20;
        public const int BackupSubsidyWeight = 20;
        public const int MinerRewardWeight = 60;
        
        public const int BasicMinerRewardWeight = 66;
        public const int VotesWeightRewardWeight = 17;
        public const int ReElectionRewardWeight = 17;

    }
}