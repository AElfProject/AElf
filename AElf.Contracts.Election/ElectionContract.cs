using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    public class ElectionContract : ElectionContractContainer.ElectionContractBase
    {
        public override Empty InitialElectionContract(InitialElectionContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.VoteContractSystemName.Value = input.VoteContractSystemName;
            State.Initialized.Value = true;
            
            State.VoteContract.Register.Send(new VotingRegisterInput
            {
                Topic = "ELF Mainchain Miners Election",
                Delegated = true,
                AcceptedCurrency = "ELF",
                ActiveDays = int.MaxValue,
                TotalEpoch = int.MaxValue
            });
            
            return new Empty();
        }

        /// <summary>
        /// Actually this method is for adding an option of voting.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty AnnounceElection(Empty input)
        {
            var publicKey = Context.RecoverPublicKey().ToHex();
            
            Assert(State.Votes[publicKey].ActiveVotes.Count == 0, "Voter can't announce election.");
            Assert(State.Candidates[publicKey] != true, "This public was either already announced or banned.");

            State.Candidates[publicKey] = true;

            // Add this alias to history information of this candidate.
            var candidateHistory = State.Histories[publicKey];
            if (candidateHistory != null)
            {
                candidateHistory.AnnouncementTransactionId = Context.TransactionId;
                State.Histories[publicKey] = candidateHistory;
            }
            else
            {
                State.Histories[publicKey] = new CandidateHistory
                {
                    AnnouncementTransactionId = Context.TransactionId
                };
            }

            // TODO: Add an option to voting event by calling Vote Contract.

            return new Empty();
        }

        public override Empty QuitElection(Empty input)
        {
            var publicKey = Context.RecoverPublicKey().ToHex();

            State.Candidates[publicKey] = null;
            
            // TODO: Remove option from voting event.

            return new Empty();
        }
    }
}