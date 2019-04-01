using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Authorization
{
    public partial class AuthorizationContract
    {
        private Miners GetMiners()
        {
            var roundNumber = State.ConsensusContract.GetCurrentRoundNumber.Call(new Empty());
            var round = State.ConsensusContract.GetRoundInformation.Call(roundNumber);
            return new Miners {PublicKeys = {round.RealTimeMinersInformation.Keys}};
        }

        private void Send(Address address, string methodName, ByteString @params)
        {
            Context.SendInline(address, methodName, @params);
        }
    }
}