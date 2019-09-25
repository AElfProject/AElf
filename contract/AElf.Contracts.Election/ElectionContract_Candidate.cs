using AElf.Contracts.MultiToken;
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

            var pubkey = recoveredPublicKey.ToHex();

            LockCandidateNativeToken();

            AddCandidateAsOption(pubkey);

            if (State.Candidates.Value.Value.Count <= GetValidationDataCenterCount())
            {
                State.DataCentersRankingList.Value.DataCenters.Add(pubkey, 0);
                RegisterCandidateToSubsidyProfitScheme();
            }

            return new Empty();
        }

        private void AnnounceElection(byte[] recoveredPublicKey)
        {
            var pubkey = recoveredPublicKey.ToHex();
            var pubkeyByteString = ByteString.CopyFrom(recoveredPublicKey);

            Assert(!State.InitialMiners.Value.Value.Contains(pubkeyByteString),
                "Initial miner cannot announce election.");

            var candidateInformation = State.CandidateInformationMap[pubkey];

            if (candidateInformation != null)
            {
                Assert(!candidateInformation.IsCurrentCandidate,
                    $"This public key already announced election. {pubkey}");
                candidateInformation.AnnouncementTransactionId = Context.TransactionId;
                candidateInformation.IsCurrentCandidate = true;
                // In this way we can keep history of current candidate, like terms, missed time slots, etc.
                State.CandidateInformationMap[pubkey] = candidateInformation;
            }
            else
            {
                Assert(!State.BlackList.Value.Value.Contains(pubkeyByteString),
                    "This candidate already marked as evil node before.");
                State.CandidateInformationMap[pubkey] = new CandidateInformation
                {
                    Pubkey = pubkey,
                    AnnouncementTransactionId = Context.TransactionId,
                    IsCurrentCandidate = true,
                    Address = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(pubkey))
                };
            }

            State.Candidates.Value.Value.Add(pubkeyByteString);
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

        private void RegisterCandidateToSubsidyProfitScheme()
        {
            if (State.ProfitContract.Value == null)
            {
                State.ProfitContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            }

            // Add 1 Shares for this candidate in subsidy profit item.
            State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
            {
                SchemeId = State.SubsidyHash.Value,
                BeneficiaryShare = new BeneficiaryShare {Beneficiary = Context.Sender, Shares = 1}
            });
        }

        #endregion

        #region QuitElection

        /// <summary>
        /// delete a option of voting,then sub the Shares from the corresponding ProfitItem 
        /// </summary>
        /// <param name="input">Empty</param>
        /// <returns></returns>
        public override Empty QuitElection(Empty input)
        {
            var recoveredPublicKey = Context.RecoverPublicKey();
            QuitElection(recoveredPublicKey);
            var pubkey = recoveredPublicKey.ToHex();

            var candidateInformation = State.CandidateInformationMap[pubkey];

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
            State.CandidateInformationMap[pubkey] = candidateInformation;

            // Remove candidate public key from the Voting Item options.
            State.VoteContract.RemoveOption.Send(new RemoveOptionInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Option = pubkey
            });

            // Remove this candidate from subsidy profit item.
            State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
            {
                SchemeId = State.SubsidyHash.Value,
                Beneficiary = Context.Sender
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
            State.DataCentersRankingList.Value.DataCenters.Remove(recoveredPublicKey.ToHex());
        }

        #endregion
    }
}