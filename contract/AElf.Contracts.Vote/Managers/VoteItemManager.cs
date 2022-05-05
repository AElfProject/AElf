using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote.Managers
{
    public class VoteItemManager:IVoteItemManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<Hash, VotingItem> _votingItemMap;

        public VoteItemManager(CSharpSmartContractContext context, MappedState<Hash, VotingItem> votingItemMap)
        {
            _context = context;
            _votingItemMap = votingItemMap;
        }
        public void AddVoteItem(Hash votingItemId, string acceptedCurrency, bool isLockToken,
            long totalSnapshotNumber, Timestamp startTimestamp, Timestamp endTimestamp, RepeatedField<string> options)
        {
            if (votingItemId == null)
            {
                throw new AssertionException("Invalid voteItem id");
            }
            if(_votingItemMap[votingItemId] != null)
            {
                throw new AssertionException("Voting item already exists.");
            };
            if (totalSnapshotNumber == 0)
            {
                totalSnapshotNumber = 1;
            }

            if (endTimestamp <= startTimestamp)
            {
                throw new AssertionException("Invalid active time.");
            }
            
            _context.LogDebug(() => $"Voting item created by {_context.Sender}: {votingItemId.ToHex()}");

            var votingItem = new VotingItem
            {
                Sponsor = _context.Sender,
                VotingItemId = votingItemId,
                AcceptedCurrency = acceptedCurrency,
                IsLockToken = isLockToken,
                TotalSnapshotNumber = totalSnapshotNumber,
                CurrentSnapshotNumber = 1,
                CurrentSnapshotStartTimestamp = startTimestamp,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                RegisterTimestamp = _context.CurrentBlockTime,
                Options = {options}
            };
            _votingItemMap[votingItemId] = votingItem;
        }
        
        public void RemoveVoteItem(Hash votingItemId)
        {
            throw new System.NotImplementedException();
        }

        public long UpdateSnapshotNumber(Hash votingItemId)
        {
            var votingItem = _votingItemMap[votingItemId];
            var nextSnapshotNumber = votingItem.CurrentSnapshotNumber.Add(1);
            votingItem.CurrentSnapshotNumber = nextSnapshotNumber;
            _votingItemMap[votingItem.VotingItemId] = votingItem;
            return nextSnapshotNumber;
        }

        public void AddOptions(Hash votingItemId, RepeatedField<string> options)
        {
            var votingItem = _votingItemMap[votingItemId];
            if (votingItem.Sponsor != _context.Sender)
            {
                throw new AssertionException("Only sponsor can update options.");
            }

            foreach (var option in options)
            {
                AssertOption(votingItem, option,false);
            }
            votingItem.Options.AddRange(options);
            if (votingItem.Options.Count > VoteContractConstants.MaximumOptionsCount)
            {
                throw new AssertionException(
                    $"The count of options can't greater than {VoteContractConstants.MaximumOptionsCount}");
            }
            _votingItemMap[votingItemId] = votingItem;
        }

        public void RemoveOptions(Hash votingItemId, RepeatedField<string> options)
        {
            var votingItem = _votingItemMap[votingItemId];
            if (votingItem.Sponsor != _context.Sender)
            {
                throw new AssertionException("Only sponsor can update options.");
            }

            foreach (var option in options)
            {
                AssertOption(votingItem,option,true);
                votingItem.Options.Remove(option);
            }

            _votingItemMap[votingItemId] = votingItem;
        }

        public VotingItem GetVotingItem(Hash votingItemId)
        {
            var votingItem = _votingItemMap[votingItemId];
            if (votingItem == null)
            {
                throw new AssertionException($"Voting item not found. {votingItemId.ToHex()}");
            }
            return votingItem;
        }

        private static void AssertOption(VotingItem votingItem, string option, bool isExistsAssert)
        {
            if (option.Length > VoteContractConstants.OptionLengthLimit)
            {
                throw new AssertionException("Invalid input.");
            }

            if (isExistsAssert)
            {
                if (!votingItem.Options.Contains(option))
                {
                    throw new AssertionException("Option doesn't exists.");
                }
            }
            else
            {
                if (votingItem.Options.Contains(option))
                {
                    throw new AssertionException("Option already exists.");
                }
            }
            
            
        }
    }
}