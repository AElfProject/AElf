using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    /// <summary>
    /// AnnounceElection & QuitElection
    /// </summary>
    public partial class ElectionContract
    {
        #region AnnounceElection

        /// <summary>
        /// Actually this method is for adding an option of the Voting Item.
        /// Thus the limitation of candidates will be limited by the capacity of voting options.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty AnnounceElection(Empty input)
        {
            var recoveredPublicKey = Context.RecoverPublicKey();
            AnnounceElection(recoveredPublicKey);

            var publicKey = recoveredPublicKey.ToHex();

            LockCandidateNativeToken();

            AddCandidateAsOption(publicKey);

            RegisterCandidateToSubsidyProfitItem();

            return new Empty();
        }

        private void AnnounceElection(byte[] recoveredPublicKey)
        {
            var publicKey = recoveredPublicKey.ToHex();
            var publicKeyByteString = ByteString.CopyFrom(recoveredPublicKey);

            // TODO: Reconsider.
            Assert(
                State.ElectorVotes[publicKey] == null || State.ElectorVotes[publicKey].ActiveVotingRecordIds == null ||
                State.ElectorVotes[publicKey].ActiveVotedVotesAmount == 0, "Voter can't announce election.");

            Assert(!State.InitialMiners.Value.Value.Contains(publicKeyByteString),
                "Initial miner cannot announce election.");

            var candidateInformation = State.CandidateInformationMap[publicKey];

            if (candidateInformation != null)
            {
                Assert(!candidateInformation.IsCurrentCandidate,
                    "This public key already announced election.");
                candidateInformation.AnnouncementTransactionId = Context.TransactionId;
                candidateInformation.IsCurrentCandidate = true;
                // In this way we can keep history of current candidate, like terms, missed time slots, etc.
                State.CandidateInformationMap[publicKey] = candidateInformation;
            }
            else
            {
                Assert(!State.BlackList.Value.Value.Contains(publicKeyByteString),
                    "This candidate already marked as evil node before.");
                State.CandidateInformationMap[publicKey] = new CandidateInformation
                {
                    Pubkey = publicKey,
                    AnnouncementTransactionId = Context.TransactionId,
                    IsCurrentCandidate = true
                };
            }

            State.Candidates.Value.Value.Add(publicKeyByteString);
        }

        private void LockCandidateNativeToken()
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            // Lock the token from sender for deposit of announce election
            State.TokenContract.Lock.Send(new LockInput
            {
                Address = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                Amount = ElectionContractConstants.LockTokenForElection,
                LockId = Context.TransactionId,
                Usage = "Lock for announcing election."
            });
        }

        private void AddCandidateAsOption(string publicKey)
        {
            if (State.VoteContract.Value == null)
            {
                State.VoteContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName);
            }

            // Add this candidate as an option for the the Voting Item.
            State.VoteContract.AddOption.Send(new AddOptionInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Option = publicKey
            });
        }

        private void RegisterCandidateToSubsidyProfitItem()
        {
            if (State.ProfitContract.Value == null)
            {
                State.ProfitContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            }

            // Add 1 weight for this candidate in subsidy profit item.
            State.ProfitContract.AddWeight.Send(new AddWeightInput
            {
                ProfitId = State.SubsidyHash.Value,
                Receiver = Context.Sender,
                Weight = 1
            });
        }

        #endregion

        #region QuitElection

        /// <summary>
        /// delete a option of voting,then sub the weight from the corresponding ProfitItem 
        /// </summary>
        /// <param name="input">Empty</param>
        /// <returns></returns>
        public override Empty QuitElection(Empty input)
        {
            var recoveredPublicKey = Context.RecoverPublicKey();
            QuitElection(recoveredPublicKey);
            var publicKey = recoveredPublicKey.ToHex();

            var candidateInformation = State.CandidateInformationMap[publicKey];

            // Unlock candidate's native token.
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Address = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                LockId = candidateInformation.AnnouncementTransactionId,
                Amount = ElectionContractConstants.LockTokenForElection,
                Usage = "Quit election."
            });

            // Update candidate information.
            candidateInformation.IsCurrentCandidate = false;
            candidateInformation.AnnouncementTransactionId = Hash.Empty;
            State.CandidateInformationMap[publicKey] = candidateInformation;

            // Remove candidate public key from the Voting Item options.
            State.VoteContract.RemoveOption.Send(new RemoveOptionInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Option = publicKey
            });

            // Remove this candidate from subsidy profit item.
            State.ProfitContract.SubWeight.Send(new SubWeightInput
            {
                ProfitId = State.SubsidyHash.Value,
                Receiver = Context.Sender
            });

            return new Empty();
        }

        private void QuitElection(byte[] recoveredPublicKey)
        {
            var publicKeyByteString = ByteString.CopyFrom(recoveredPublicKey);

            Assert(State.Candidates.Value.Value.Contains(publicKeyByteString), "Sender is not a candidate.");

            if (State.AEDPoSContract.Value == null)
            {
                State.AEDPoSContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }

            Assert(
                !State.AEDPoSContract.GetCurrentMinerList.Call(new Empty()).Pubkeys
                    .Contains(publicKeyByteString),
                "Current miners cannot quit election.");

            State.Candidates.Value.Value.Remove(publicKeyByteString);
        }

        #endregion
    }
}