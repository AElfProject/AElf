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
            State.TokenContractSystemName.Value = input.TokenContractSystemName;
            State.Initialized.Value = true;

            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = ElectionContractConsts.VoteSymbol,
                TokenName = "Elf Vote token",
                Issuer = Context.Self,
                Decimals = 2,
                IsBurnable = true,
                TotalSupply = ElectionContractConsts.VotesTotalSupply,
                LockWhiteList = {Context.Self}
            });

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = ElectionContractConsts.Symbol,
                Amount = ElectionContractConsts.VotesTotalSupply,
                To = Context.Self,
                Memo = "Power!"
            });

            State.VoteContract.Register.Send(new VotingRegisterInput
            {
                Topic = ElectionContractConsts.Topic,
                Delegated = true,
                AcceptedCurrency = ElectionContractConsts.Symbol,
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

            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = ElectionContractConsts.Symbol,
                Amount = ElectionContractConsts.LockTokenForElection,
                LockId = Context.TransactionId,
                Usage = "Lock for announcing election."
            });

            State.VoteContract.AddOption.Send(new AddOptionInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = Context.Self,
                Option = publicKey
            });

            return new Empty();
        }

        public override Empty QuitElection(Empty input)
        {
            var publicKey = Context.RecoverPublicKey().ToHex();

            State.Candidates[publicKey] = null;

            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = ElectionContractConsts.Symbol,
                LockId = State.Histories[publicKey].AnnouncementTransactionId,
                Amount = ElectionContractConsts.LockTokenForElection,
                Usage = "Quit election."
            });

            State.VoteContract.RemoveOption.Send(new RemoveOptionInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = Context.Self,
                Option = publicKey
            });

            return new Empty();
        }

        public override Empty Vote(VoteMinerInput input)
        {
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = ElectionContractConsts.VoteSymbol,
                To = Context.Sender,
                Amount = input.Amount,
                Memo = "Get VOTEs."
            });

            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                Symbol = ElectionContractConsts.Symbol,
                LockId = Context.TransactionId,
                Amount = input.Amount,
                To = Context.Self,
                Usage = $"Voting for {ElectionContractConsts.Topic}"
            });

            State.VoteContract.Vote.Send(new VoteInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = Context.Self,
                Amount = input.Amount,
                Option = input.CandidatePublicKey,
                Voter = Context.Sender,
                VoteId = Context.TransactionId
            });

            return new Empty();
        }

        public override Empty Withdraw(Hash input)
        {
            var votingRecord = State.VoteContract.GetVotingRecord.Call(input);
            
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = votingRecord.Voter,
                Symbol = votingRecord.Currency,
                Amount = votingRecord.Amount,
                LockId = input,
                To = votingRecord.Sponsor,
                Usage = $"Withdraw votes for {ElectionContractConsts.Topic}"
            });
            
            State.VoteContract.Withdraw.Send(new WithdrawInput
            {
                VoteId = input
            });

            return new Empty();
        }

        public override Empty UpdateTermNumber(UpdateTermNumberInput input)
        {
            State.VoteContract.UpdateEpochNumber.Send(new UpdateEpochNumberInput
            {
                EpochNumber = input.TermNumber,
                Topic = ElectionContractConsts.Topic
            });

            return new Empty();
        }

        public override ElectionResult GetElectionResult(GetElectionResultInput input)
        {
            var votingResult = State.VoteContract.GetVotingResult.Call(new GetVotingResultInput
            {
                Topic = ElectionContractConsts.Topic,
                EpochNumber = input.TermNumber,
                Sponsor = Context.Self
            });

            var result = new ElectionResult
            {
                TermNumber = input.TermNumber,
                IsActive = input.TermNumber == State.CurrentTermNumber.Value,
                Results = {votingResult.Results}
            };

            return result;
        }
    }
}