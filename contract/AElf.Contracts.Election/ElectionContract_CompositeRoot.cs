using AElf.Contracts.Election.Managers;
using AElf.Contracts.Election.Services;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract
    {
        private CandidateManager GetCandidateManager()
        {
            return new CandidateManager(Context, State.CandidateInformationMap, State.Candidates,
                State.BannedPubkeyMap, State.InitialMiners, State.CandidateAdmins, State.CandidateReplacementMap);
        }

        private LockTimeManager GetLockTimeManager()
        {
            return new LockTimeManager(Context, State.LockTimeMap);
        }

        private ElectionService GetElectionService(CandidateManager candidateManager = null)
        {
            if (candidateManager == null)
            {
                candidateManager = GetCandidateManager();
            }

            return new ElectionService(Context, candidateManager);
        }
    }
}