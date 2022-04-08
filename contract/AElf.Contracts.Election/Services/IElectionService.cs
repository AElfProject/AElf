using AElf.Types;

namespace AElf.Contracts.Election.Services
{
    public interface IElectionService
    {
        void Involve(byte[] candidatePubkey, Address admin);
        void Quit(string pubkey);
        Hash Vote(string electorPubkey, string candidatePubkey);
        void Withdraw(Hash voteId);
    }
}