using AElf.Kernel;

namespace AElf.Contracts.Authorization2
{
    public partial class AuthorizationContract
    {
        private Miners GetMiners()
        {
            var roundNumber = State.ConsensusContract.GetCurrentRoundNumber();
            var round = State.ConsensusContract.GetRoundInfo(roundNumber);
            return new Miners {PublicKeys = {round.RealTimeMinersInfo.Keys}};
        }
    }
}