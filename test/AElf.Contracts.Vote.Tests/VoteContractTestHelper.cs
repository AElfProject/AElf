using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Contracts.Vote
{
    public partial class VoteTests : VoteContractTestBase
    {
        public VoteTests()
        {
            InitializeContracts();
        }

        /// <summary>
        /// Register a voting item.
        /// </summary>
        /// <param name="lastingDays"></param>
        /// <param name="optionsCount"></param>
        /// <param name="isLockToken"></param>
        /// <param name="sender"></param>
        /// <param name="totalSnapshotNumber"></param>
        /// <returns></returns>
        private async Task<VotingItem> RegisterVotingItemAsync(int lastingDays, int optionsCount, bool isLockToken, Address sender,
            int totalSnapshotNumber = int.MaxValue)
        {
            var startTime = TimestampHelper.GetUtcNow();
            var input = new VotingRegisterInput
            {
                TotalSnapshotNumber = totalSnapshotNumber,
                EndTimestamp = startTime.AddDays(lastingDays),
                StartTimestamp = startTime,
                Options = {GenerateOptions(optionsCount)},
                AcceptedCurrency = TestTokenSymbol,
                IsLockToken = isLockToken
            };
            var transactionResult = (await VoteContractStub.Register.SendAsync(input)).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            input.Options.Clear();
            var votingItemId = Hash.FromTwoHashes(Hash.FromMessage(input), Hash.FromMessage(sender));
            return await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
                {VotingItemId = votingItemId});
        }

        private async Task<TransactionResult> TakeSnapshot(Hash votingItemId, long snapshotNumber)
        {
            var input = new TakeSnapshotInput
            {
                VotingItemId = votingItemId,
                SnapshotNumber = snapshotNumber
            };
            var transactionResult = (await VoteContractStub.TakeSnapshot.SendAsync(input)).TransactionResult;

            return transactionResult;
        }

        private async Task<TransactionResult> Withdraw(ECKeyPair owner, Hash voteId)
        {
            var voterStub = GetVoteContractTester(owner);
            var input = new WithdrawInput
            {
                VoteId = voteId
            };

            var withdrawResult = (await voterStub.Withdraw.SendAsync(input)).TransactionResult;

            return withdrawResult;
        } 
        
        private async Task<TransactionResult> Vote(ECKeyPair voterKeyPair, Hash votingItemId, string option,
            long amount)
        {
            return (await GetVoteContractTester(voterKeyPair).Vote.SendAsync(new VoteInput
            {
                VotingItemId = votingItemId,
                Option = option,
                Amount = amount
            })).TransactionResult;
        }

        private async Task<VotingRecord> GetVotingRecord(Hash voteHash)
        {
            return await VoteContractStub.GetVotingRecord.CallAsync(voteHash);
        }

        private async Task<VotingResult> GetVotingResult(Hash votingItemId, long snapshotNumber)
        {
            return await VoteContractStub.GetVotingResult.CallAsync(new GetVotingResultInput
            {
                VotingItemId = votingItemId,
                SnapshotNumber = snapshotNumber
            });
        }
            

        private async Task<VotingItem> GetVoteItem(Hash votingItemId)
        {
            return await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = votingItemId
            });
        }
        
        private async Task<VotedIds> GetVoteIds(ECKeyPair voterKeyPair, Hash votingItemId)
        {
            var voterStub = GetVoteContractTester(voterKeyPair);

            var votingIds = await voterStub.GetVotingIds.CallAsync(new GetVotingIdsInput
            {
                Voter = Address.FromPublicKey(voterKeyPair.PublicKey),
                VotingItemId = votingItemId
            });

            return votingIds;
        }
        
        private List<string> GenerateOptions(int count = 1)
        {
            return Enumerable.Range(0, count).Select(i => SampleAddress.AddressList[i].GetFormatted()).ToList();
        }
    }
}