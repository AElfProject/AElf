using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract : ElectionContractContainer.ElectionContractBase
    {
        public override Empty InitialElectionContract(InitialElectionContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.VoteContractSystemName.Value = input.VoteContractSystemName;
            State.ProfitContractSystemName.Value = input.ProfitContractSystemName;
            State.TokenContractSystemName.Value = input.TokenContractSystemName;
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty RegisterElectionVotingEvent(RegisterElectionVotingEventInput input)
        {
            Assert(!State.VotingEventRegistered.Value, "Already registered.");
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();

            State.TokenContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
            State.VoteContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.VoteContractSystemName.Value);

            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = ElectionContractConsts.VoteSymbol,
                TokenName = "Vote token",
                Issuer = Context.Self,
                Decimals = 2,
                IsBurnable = true,
                TotalSupply = ElectionContractConsts.VotesTotalSupply,
                LockWhiteList = {Context.Self}
            });

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = ElectionContractConsts.VoteSymbol,
                Amount = ElectionContractConsts.VotesTotalSupply,
                To = Context.Self,
                Memo = "Power!"
            });

            State.VoteContract.Register.Send(new VotingRegisterInput
            {
                Topic = ElectionContractConsts.Topic,
                Delegated = true,
                AcceptedCurrency = Context.Variables.NativeSymbol,
                ActiveDays = long.MaxValue,
                TotalEpoch = long.MaxValue
            });

            State.VotingEventRegistered.Value = true;
            return new Empty();
        }

        public override Empty CreateTreasury(CreateTreasuryInput input)
        {
            Assert(!State.TreasuryCreated.Value, "Already created.");

            State.ProfitContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ProfitContractSystemName.Value);

            // Create profit items: `Treasury`, `CitizenWelfare`, `BackupSubsidy`, `MinerReward`,
            // `MinerBasicReward`, `MinerVotesWeightReward`, `ReElectionMinerReward`
            for (var i = 0; i < 7; i++)
            {
                State.ProfitContract.CreateProfitItem.Send(new CreateProfitItemInput
                {
                    TokenSymbol = Context.Variables.NativeSymbol
                });
            }

            State.TreasuryCreated.Value = true;

            return new Empty();
        }

        public override Empty RegisterToTreasury(RegisterToTreasuryInput input)
        {
            Assert(!State.TreasuryRegistered.Value, "Already created.");

            var createdProfitIds = State.ProfitContract.GetCreatedProfitItems.Call(new GetCreatedProfitItemsInput
            {
                Creator = Context.Self
            }).ProfitIds;

            Assert(createdProfitIds.Count == 7, "Incorrect profit items count.");

            State.TreasuryHash.Value = createdProfitIds[0];
            State.WelfareHash.Value = createdProfitIds[1];
            State.SubsidyHash.Value = createdProfitIds[2];
            State.RewardHash.Value = createdProfitIds[3];
            State.BasicRewardHash.Value = createdProfitIds[4];
            State.VotesWeightRewardHash.Value = createdProfitIds[5];
            State.ReElectionRewardHash.Value = createdProfitIds[6];

            // Add profits to `Treasury`
            State.ProfitContract.AddProfits.Send(new AddProfitsInput
            {
                ProfitId = State.TreasuryHash.Value,
                Amount = ElectionContractConsts.VotesTotalSupply
            });

            // Register `CitizenWelfare` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.WelfareHash.Value,
                SubItemWeight = 20
            });

            // Register `BackupSubsidy` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.SubsidyHash.Value,
                SubItemWeight = 20
            });

            // Register `MinerReward` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.RewardHash.Value,
                SubItemWeight = 60
            });

            // Register `MinerBasicReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.BasicRewardHash.Value,
                SubItemWeight = 66
            });

            // Register `MinerVotesWeightReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.VotesWeightRewardHash.Value,
                SubItemWeight = 17
            });

            // Register `ReElectionMinerReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.ReElectionRewardHash.Value,
                SubItemWeight = 17
            });

            State.TreasuryRegistered.Value = true;

            return new Empty();
        }

        public override Empty ReleaseTreasuryProfits(ReleaseTreasuryProfitsInput input)
        {
            var createdProfitIds = State.ProfitContract.GetCreatedProfitItems.Call(new GetCreatedProfitItemsInput
            {
                Creator = Context.Self
            }).ProfitIds;

            Assert(createdProfitIds.Count >= 7, "Incorrect profit items count.");

            var totalReleasedAmount = input.MinedBlocks.Mul(ElectionContractConsts.ElfTokenPerBlock);
            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.TreasuryHash.Value,
                Amount = totalReleasedAmount,
                Period = input.TermNumber
            });

            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.RewardHash.Value,
                Amount = totalReleasedAmount.Mul(60).Div(100),
                Period = input.TermNumber
            });

            // Update epoch of voting record btw.
            State.VoteContract.UpdateEpochNumber.Send(new UpdateEpochNumberInput
            {
                EpochNumber = input.TermNumber,
                Topic = ElectionContractConsts.Topic
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

            Assert(
                State.Votes[publicKey] == null || State.Votes[publicKey].ActiveVotesIds == null ||
                State.Votes[publicKey].ActiveVotesIds.Count == 0, "Voter can't announce election.");

            // Add this alias to history information of this candidate.
            var candidateHistory = State.Histories[publicKey];

            if (candidateHistory != null)
            {
                Assert(candidateHistory.State != CandidateState.IsEvilNode,
                    "This candidate already marked as evil node before.");
                Assert(candidateHistory.State == CandidateState.NotAnnounced,
                    "This public key already announced election.");
                candidateHistory.AnnouncementTransactionId = Context.TransactionId;
                State.Histories[publicKey] = candidateHistory;
            }
            else
            {
                State.Histories[publicKey] = new CandidateHistory
                {
                    AnnouncementTransactionId = Context.TransactionId,
                    State = CandidateState.IsCandidate
                };
            }

            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
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

            State.ProfitContract.AddWeight.Send(new AddWeightInput
            {
                ProfitId = State.SubsidyHash.Value,
                Receiver = Context.Sender,
                Weight = 1
            });

            return new Empty();
        }

        public override Empty QuitElection(Empty input)
        {
            var publicKey = Context.RecoverPublicKey().ToHex();

            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
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

            var candidateHistory = State.Histories[publicKey];
            candidateHistory.State = CandidateState.NotAnnounced;
            State.Histories[publicKey] = candidateHistory;

            State.ProfitContract.SubWeight.Send(new SubWeightInput
            {
                ProfitId = State.SubsidyHash.Value,
                Receiver = Context.Sender
            });

            return new Empty();
        }

        public override Empty Vote(VoteMinerInput input)
        {
            var lockTime = input.LockTimeUnit == LockTimeUnit.Days ? input.LockTime : input.LockTime * 30;
            Assert(lockTime >= 90, "Should lock token for at least 90 days.");
            State.LockTimeMap[Context.TransactionId] = lockTime;

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
                Symbol = Context.Variables.NativeSymbol,
                LockId = Context.TransactionId,
                Amount = input.Amount,
                To = Context.Self,
                Usage = "Voting for Mainchain Election."
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

            State.ProfitContract.AddWeight.Send(new AddWeightInput
            {
                ProfitId = State.WelfareHash.Value,
                Receiver = Context.Sender,
                Weight = GetVotesWeight(input.Amount, lockTime)
            });

            return new Empty();
        }

        public override Empty Withdraw(Hash input)
        {
            var votingRecord = State.VoteContract.GetVotingRecord.Call(input);

            var actualLockedDays = (Context.CurrentBlockTime - votingRecord.VoteTimestamp.ToDateTime()).TotalDays;
            var claimedLockDays = State.LockTimeMap[input];
            Assert(actualLockedDays >= claimedLockDays,
                $"Still need {claimedLockDays - actualLockedDays} days to unlock your token.");

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

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            return (long) (((double) lockTime / 270 + 2.0 / 3.0) * votesAmount);
        }
    }
}