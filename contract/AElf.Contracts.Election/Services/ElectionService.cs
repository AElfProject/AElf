using AElf.Contracts.Election.Managers;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.Election.Services
{
    public class ElectionService : IElectionService
    {
        private readonly CSharpSmartContractContext _context;
        private readonly ICandidateManager _candidateManager;

        public ElectionService(CSharpSmartContractContext context, ICandidateManager candidateManager)
        {
            _context = context;
            _candidateManager = candidateManager;
        }

        public void Involve(byte[] candidatePubkey, Address admin)
        {
            _candidateManager.AddCandidate(candidatePubkey, admin);
        }

        public void Quit(string pubkey)
        {
            throw new System.NotImplementedException();
        }

        public Hash Vote(string electorPubkey, string candidatePubkey)
        {
            throw new System.NotImplementedException();
        }

        public void Withdraw(Hash voteId)
        {
            throw new System.NotImplementedException();
        }
    }
}