using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.LuckGame
{
    public partial class LuckGameContract : LuckGameContractContainer.LuckGameContractBase
    {
        public override Empty InitializeLuckGame(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.RoundNumber.Value = 0;

            return new Empty();
        }

        public override BetInfo PlayBet(BetInput input)
        {
            var roundNumber = State.RoundNumber.Value + 1;
            var sender = Context.Sender;
            var result = State.UserPlayRecordId[roundNumber][sender];
            
            Assert(result == null, "Each round only allow to play one time.");

            var playHash = GenerateBetId(input);
            State.UserPlayRecordId[roundNumber][sender] = playHash;

            return new BetInfo
            {
                Round = roundNumber,
                BetId = playHash
            };
        }
    }
}