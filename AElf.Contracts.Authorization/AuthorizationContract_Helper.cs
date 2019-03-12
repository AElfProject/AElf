using AElf.Consensus.DPoS;

namespace AElf.Contracts.Authorization
{
    public partial class AuthorizationContract
    {
        private Miners GetMiners()
        {
            var roundNumber = State.ConsensusContract.GetCurrentRoundNumber();
            var round = State.ConsensusContract.GetRoundInfo(roundNumber);
            return new Miners {PublicKeys = {round.RealTimeMinersInformation.Keys}};
        }
    }
}