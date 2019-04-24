namespace AElf.Contracts.Election
{
    public class ElectionContractConsts
    {
        public static readonly Hash Topic = Hash.FromString("Mainchain Election");
        public const long LockTokenForElection = 100_000;
        public const string VoteSymbol = "VOTE";
        public const long VotesTotalSupply = 200_000_000;
        public const long ElfTokenPerBlock = 1;
    }
}