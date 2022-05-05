using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElf.Sdk.CSharp;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote
{
    /// <summary>
    /// Comments and documents see README.md of current project.
    /// </summary>
    public partial class VoteContract : VoteContractImplContainer.VoteContractImplBase
    {
        /// <summary>
        /// To register a new voting item while filling up with details.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Register(VotingRegisterInput input)
        {
            MakeSureReferenceStateAddressSet(State.TokenContract,SmartContractConstants.TokenContractSystemName);
            var votingItemId = GenerateVoteItemId(input);
            GetVoteService().RegisterVote(votingItemId, input.AcceptedCurrency, input.IsLockToken, input.TotalSnapshotNumber, input.StartTimestamp, input.EndTimestamp, input.Options);

            return new Empty();
        }

        /// <summary>
        /// Execute the Vote action,save the VoteRecords and update the VotingResults and the VotedItems
        /// Before Voting,the VotingItem's token must be locked,except the votes delegated to a contract.
        /// </summary>
        /// <param name="input">VoteInput</param>
        /// <returns></returns>
        public override Empty Vote(VoteInput input)
        {
            MakeSureReferenceStateAddressSet(State.TokenContract,SmartContractConstants.TokenContractSystemName);
            GetVoteService().Vote(input.VoteId, input.VotingItemId, input.Amount,input.Option,input.Voter,input.IsChangeTarget);
            
            return new Empty();
        }


        /// <summary>
        /// Withdraw the Votes.
        /// first,mark the related record IsWithdrawn.
        /// second,delete the vote form ActiveVotes and add the vote to withdrawnVotes.
        /// finally,unlock the token that Locked in the VotingItem 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Withdraw(WithdrawInput input)
        {
            MakeSureReferenceStateAddressSet(State.TokenContract,SmartContractConstants.TokenContractSystemName);
            GetVoteService().Withdraw(input.VoteId);
            return new Empty();
        }

        public override Empty TakeSnapshot(TakeSnapshotInput input)
        {
            GetVoteService().TakeSnapshot(input.VotingItemId, input.SnapshotNumber);
            return new Empty();
        }

        /// <summary>
        /// Add a option for corresponding VotingItem.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty AddOption(AddOptionInput input)
        {
            GetVoteItemManager().GetVotingItem(input.VotingItemId);
            GetVoteItemManager().AddOptions(input.VotingItemId,new RepeatedField<string>{input.Option});
            return new Empty();
        }

        private void AssertOption(VotingItem votingItem, string option)
        {
            Assert(option.Length <= VoteContractConstants.OptionLengthLimit, "Invalid input.");
            Assert(!votingItem.Options.Contains(option), "Option already exists.");
        }

        /// <summary>
        /// Delete a option for corresponding VotingItem
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty RemoveOption(RemoveOptionInput input)
        {
            GetVoteItemManager().GetVotingItem(input.VotingItemId);
            GetVoteItemManager().RemoveOptions(input.VotingItemId,new RepeatedField<string>{input.Option});
            return new Empty();
        }

        public override Empty AddOptions(AddOptionsInput input)
        {
            GetVoteItemManager().GetVotingItem(input.VotingItemId);
            GetVoteItemManager().AddOptions(input.VotingItemId,input.Options);
            return new Empty();
        }

        public override Empty RemoveOptions(RemoveOptionsInput input)
        {
            GetVoteItemManager().GetVotingItem(input.VotingItemId);
            GetVoteItemManager().RemoveOptions(input.VotingItemId,input.Options);
            return new Empty();
        }

        private VotingItem AssertVotingItem(Hash votingItemId)
        {
            var votingItem = State.VotingItems[votingItemId];
            Assert(votingItem != null, $"Voting item not found. {votingItemId.ToHex()}");
            return votingItem;
        }

    }
}