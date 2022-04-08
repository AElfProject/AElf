using AElf.Types;

namespace AElf.Contracts.Election.Managers
{
    public interface ICandidateManager
    {
        void AddCandidate(byte[] candidatePubkey, Address admin);

        CandidateInformation GetCandidateInformation(string pubkey);
        void AssertCandidateValid(string pubkey);
    }
}