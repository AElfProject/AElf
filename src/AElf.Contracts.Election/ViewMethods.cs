using AElf.Kernel;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract
    {
        public override ElectionTickets GetTicketsInformation(StringInput input)
        {
            var votingRecords = State.VoteContract.GetVotingHistory.Call(new GetVotingHistoryInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = Context.Self,
                Voter = Address.FromPublicKey(ByteArrayHelpers.FromHexString(input.Value))
            });
            
            var electionTickets = new ElectionTickets
            {
                PublicKey = input.Value,
                
            };

            return electionTickets;
        }
    }
}