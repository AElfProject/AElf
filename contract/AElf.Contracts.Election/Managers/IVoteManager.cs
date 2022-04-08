namespace AElf.Contracts.Election.Managers
{
    public interface IVoteManager
    {
        void AddOption(string pubkey);
        void AddVote();
    }
}