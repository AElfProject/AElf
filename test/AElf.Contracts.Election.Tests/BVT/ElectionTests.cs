using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic;
using AElf.Standards.ACS3;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        public const int CandidatesCount = 19;

        [Fact]
        public async Task ElectionContract_RegisterElectionVotingEvent_Test()
        {
            // `RegisterElectionVotingEvent` will be called during AElf Consensus Contract initialization,
            // so we can check corresponding voting item directly.

            var electionVotingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = MinerElectionVotingItemId
            });

            electionVotingItem.VotingItemId.ShouldBe(MinerElectionVotingItemId);
            electionVotingItem.Options.Count.ShouldBe(0);
            electionVotingItem.Sponsor.ShouldBe(ElectionContractAddress);
            electionVotingItem.TotalSnapshotNumber.ShouldBe(long.MaxValue);
            electionVotingItem.CurrentSnapshotNumber.ShouldBe(1);
            electionVotingItem.IsLockToken.ShouldBe(false);
            electionVotingItem.AcceptedCurrency.ShouldBe(ElectionContractTestConstants.NativeTokenSymbol);
        }

        [Fact]
        public async Task ElectionContract_AnnounceElection_State_Test()
        {
            var candidatesKeyPair = ValidationDataCenterKeyPairs.First();
            var balanceBeforeAnnouncing = await GetNativeTokenBalance(candidatesKeyPair.PublicKey);
            await AnnounceElectionAsync(candidatesKeyPair);
            var balanceAfterAnnouncing = await GetNativeTokenBalance(candidatesKeyPair.PublicKey);
            balanceAfterAnnouncing.ShouldBe(balanceBeforeAnnouncing - ElectionContractConstants.LockTokenForElection);
            var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = MinerElectionVotingItemId
            });
            votingItem.Options.Count.ShouldBe(1);
            votingItem.Options.ShouldContain(candidatesKeyPair.PublicKey.ToHex());
            var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterList.DataCenters.ContainsKey(candidatesKeyPair.PublicKey.ToHex()).ShouldBeTrue();
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            var profitDetail = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(candidatesKeyPair.PublicKey)
            });
            profitDetail.Details.Count.ShouldBe(1);
        }

        /// <summary>
        /// Take first 7 full node key pairs to announce election.
        /// </summary>
        /// <returns>Return 7 candidates key pairs.</returns>
        private async Task<List<ECKeyPair>> ElectionContract_AnnounceElection_Test()
        {
            var candidatesKeyPairs = ValidationDataCenterKeyPairs.Take(CandidatesCount).ToList();

            var balanceBeforeAnnouncing = await GetNativeTokenBalance(candidatesKeyPairs[0].PublicKey);
            balanceBeforeAnnouncing.ShouldBe(ElectionContractConstants.UserInitializeTokenAmount);

            foreach (var keyPair in candidatesKeyPairs)
            {
                await AnnounceElectionAsync(keyPair);
            }

            var balanceAfterAnnouncing = await GetNativeTokenBalance(candidatesKeyPairs[0].PublicKey);

            // Check balance after announcing election.
            balanceBeforeAnnouncing.ShouldBe(balanceAfterAnnouncing + ElectionContractConstants.LockTokenForElection);

            // Check changes introduced to Main Chain Miner Election voting item.
            var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = MinerElectionVotingItemId
            });
            votingItem.Options.Count.ShouldBe(CandidatesCount);
            foreach (var candidateKeyPair in candidatesKeyPairs)
            {
                votingItem.Options.ShouldContain(candidateKeyPair.PublicKey.ToHex());
            }

            return candidatesKeyPairs;
        }

        [Fact]
        public async Task ElectionContract_AnnounceElectionAgain_Test()
        {
            await ElectionContract_QuiteElection_Test();
            
            var candidatesKeyPair = ValidationDataCenterKeyPairs.First();

            var balanceBeforeAnnouncing = await GetNativeTokenBalance(candidatesKeyPair.PublicKey);
            balanceBeforeAnnouncing.ShouldBe(ElectionContractConstants.UserInitializeTokenAmount);

            await AnnounceElectionAsync(candidatesKeyPair);

            var balanceAfterAnnouncing = await GetNativeTokenBalance(candidatesKeyPair.PublicKey);

            // Check balance after announcing election.
            balanceBeforeAnnouncing.ShouldBe(balanceAfterAnnouncing + ElectionContractConstants.LockTokenForElection);
        }

        private async Task ElectionContract_QuiteElection_Test()
        {
            const int quitCount = 2;

            var candidates = await ElectionContract_AnnounceElection_Test();

            // Check VotingEvent before quiting election.
            {
                var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
                {
                    VotingItemId = MinerElectionVotingItemId
                });
                votingItem.Options.Count.ShouldBe(candidates.Count);
            }

            var quitCandidates = ValidationDataCenterKeyPairs.Take(quitCount).ToList();

            var balancesBeforeQuiting = new Dictionary<ECKeyPair, long>();
            // Record balances before quiting election.
            foreach (var quitCandidate in quitCandidates)
            {
                balancesBeforeQuiting.Add(quitCandidate, await GetNativeTokenBalance(quitCandidate.PublicKey));
            }

            foreach (var keyPair in quitCandidates)
            {
                await QuitElectionAsync(keyPair);
            }

            // Check balances after quiting election.
            foreach (var quitCandidate in quitCandidates)
            {
                var balance = await GetNativeTokenBalance(quitCandidate.PublicKey);
                balance.ShouldBe(balancesBeforeQuiting[quitCandidate] + ElectionContractConstants.LockTokenForElection);
            }

            // Check VotingEvent after quiting election.
            {
                var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
                {
                    VotingItemId = MinerElectionVotingItemId
                });
                votingItem.Options.Count.ShouldBe(candidates.Count - quitCount);
            }
        }
        
        [Fact]
        public async Task ElectionContract_QuiteElection_State_Test()
        {
            var candidatesKeyPair = ValidationDataCenterKeyPairs.First();
            await AnnounceElectionAsync(candidatesKeyPair);
            var balanceBeforeQuit = await GetNativeTokenBalance(candidatesKeyPair.PublicKey);
            await QuitElectionAsync(candidatesKeyPair);
            var balanceAfterQuit = await GetNativeTokenBalance(candidatesKeyPair.PublicKey);
            balanceAfterQuit.ShouldBe(balanceBeforeQuit + ElectionContractConstants.LockTokenForElection);
            var votingItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
            {
                VotingItemId = MinerElectionVotingItemId
            });
            votingItem.Options.Count.ShouldBe(0);
            var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterList.DataCenters.Count.ShouldBe(0);
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            var profitDetail = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(candidatesKeyPair.PublicKey)
            });
            profitDetail.Details.Count.ShouldBe(1);
            profitDetail.Details[0].EndPeriod.ShouldBe(0);
        }

        /// <summary>
        /// TokenContract dose have not enough vote token to issue
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ElectionContract_Vote_Without_Enough_VoteToken_Test()
        {
            var voteSymbol = "VOTE";
            var voteTokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = voteSymbol
            });
            var amount = voteTokenInfo.TotalSupply.Div(10).Mul(5);
            var voterKeyPair = VoterKeyPairs.First();
            var issueRet = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
            {
                Amount = amount,
                To = Address.FromPublicKey(voterKeyPair.PublicKey)
            });
            issueRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            const int lockTime = 100 * 60 * 60 * 24;
            var candidatesKeyPairs = await ElectionContract_AnnounceElection_Test();
            var candidateKeyPair = candidatesKeyPairs[0];
            var voteRet = await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), lockTime, amount);
            voteRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var voteId =
                (await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                    {Value = voterKeyPair.PublicKey.ToHex()})).ActiveVotingRecordIds.First();
            BlockTimeProvider.SetBlockTime(StartTimestamp.AddSeconds(lockTime + 1));
            await WithdrawVotes(voterKeyPair, voteId);
            var voteBalanceInElectionContract = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = voteSymbol,
                Owner = ElectionContractAddress
            });
            voteBalanceInElectionContract.Balance.ShouldBe(voteTokenInfo.TotalSupply);
            
            voteRet = await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), lockTime * 2, amount);
            voteRet.Status.ShouldBe(TransactionResultStatus.Mined);
            voteBalanceInElectionContract = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = voteSymbol,
                Owner = ElectionContractAddress
            });
            voteBalanceInElectionContract.Balance.ShouldBe(amount);
        }
        
        [Fact]
        public async Task ElectionContract_Vote_With_Token_Test()
        {
            var amount = 100;
            const int lockTime = 100 * 60 * 60 * 24;
            var candidatesKeyPairs = await ElectionContract_AnnounceElection_Test();
            var candidateKeyPair = candidatesKeyPairs[0];
            var voteRet = await ElectionContractStub.Vote.SendAsync(new VoteMinerInput
            {
                CandidatePubkey = candidateKeyPair.PublicKey.ToHex(),
                Amount = amount,
                EndTimestamp = TimestampHelper.GetUtcNow().AddSeconds(lockTime),
                Token = HashHelper.ComputeFrom("token A")
            });
            voteRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        
        /// <summary>
        /// First 5 candidates will get 500 * 2 votes, next 14 candidates will get 250 * 2 votes.
        /// Votes are got from 2 different voters.
        /// </summary>
        /// <returns></returns>
        public async Task<List<ECKeyPair>> ElectionContract_Vote_Test()
        {
            const int votersCount = 2;
            const long amount = 500;
            const int lockTime = 100 * 60 * 60 * 24;

            var candidatesKeyPairs = await ElectionContract_AnnounceElection_Test();
            var candidateKeyPair = candidatesKeyPairs[0];

            var votersKeyPairs = VoterKeyPairs.Take(votersCount).ToList();
            var voterKeyPair = votersKeyPairs[0];
            var balanceBeforeVoting = await GetNativeTokenBalance(voterKeyPair.PublicKey);
            balanceBeforeVoting.ShouldBeGreaterThan(0);

            await VoteToCandidates(
                votersKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList(),
                candidatesKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(), lockTime, amount);

            await VoteToCandidates(
                votersKeyPairs.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
                    .Take(candidatesKeyPairs.Count - EconomicContractsTestConstants.InitialCoreDataCenterCount)
                    .ToList(),
                candidatesKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(), lockTime, amount / 2);

            var actualVotedAmount =
                amount * EconomicContractsTestConstants.InitialCoreDataCenterCount + amount *
                (candidatesKeyPairs.Count - EconomicContractsTestConstants.InitialCoreDataCenterCount);

            // Check ELF token balance.
            {
                var balance = await GetNativeTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(balanceBeforeVoting - actualVotedAmount);
            }

            // Check VOTE token balance.
            {
                var balance = await GetVoteTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(actualVotedAmount);
            }

            // Check voter's Votes information.
            {
                var voterVotes = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                {
                    Value = voterKeyPair.PublicKey.ToHex()
                });
                voterVotes.Pubkey.ShouldBe(ByteString.CopyFrom(voterKeyPair.PublicKey));
                voterVotes.ActiveVotingRecordIds.Count.ShouldBe(19);
                voterVotes.AllVotedVotesAmount.ShouldBe(actualVotedAmount);
                voterVotes.ActiveVotedVotesAmount.ShouldBe(actualVotedAmount);
                voterVotes.ActiveVotingRecords.Count.ShouldBe(0); // Not filled.

                var voterVotesWithRecords = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(
                    new StringValue
                    {
                        Value = voterKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithRecords.ActiveVotingRecords.Count.ShouldBe(candidatesKeyPairs.Count);

                var voterVotesWithAllRecords = await ElectionContractStub.GetElectorVoteWithAllRecords.CallAsync(
                    new StringValue
                    {
                        Value = voterKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithAllRecords.WithdrawnVotesRecords.Count.ShouldBe(0);
            }

            // Check candidate's Votes information.
            {
                //not exist
                var input = new StringValue
                {
                    Value = "FakePubkey"
                };
                var candidateVotesWithRecords = await ElectionContractStub.GetCandidateVoteWithRecords.CallAsync(input);
                candidateVotesWithRecords.ShouldBe(new CandidateVote());
                
                var candidateVotes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
                {
                    Value = candidateKeyPair.PublicKey.ToHex()
                });
                candidateVotes.Pubkey.ShouldBe(ByteString.CopyFrom(candidateKeyPair.PublicKey));
                candidateVotes.AllObtainedVotedVotesAmount.ShouldBe(amount * 2);
                candidateVotes.ObtainedActiveVotedVotesAmount.ShouldBe(amount * 2);
                candidateVotes.ObtainedWithdrawnVotesRecords.Count.ShouldBe(0); // Not filled.

                candidateVotesWithRecords = await ElectionContractStub.GetCandidateVoteWithRecords.CallAsync(
                    new StringValue
                    {
                        Value = candidateKeyPair.PublicKey.ToHex()
                    });
                candidateVotesWithRecords.ObtainedActiveVotingRecords.Count.ShouldBe(votersCount);

                var voterVotesWithAllRecords = await ElectionContractStub.GetCandidateVoteWithAllRecords.CallAsync(
                    new StringValue
                    {
                        Value = candidateKeyPair.PublicKey.ToHex()
                    });
                voterVotesWithAllRecords.ObtainedWithdrawnVotesRecords.Count.ShouldBe(0);
            }

            // Check voter's profit detail.
            {
                var welfareHash = ProfitItemsIds[ProfitType.CitizenWelfare];
                var details = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = welfareHash,
                    Beneficiary = Address.FromPublicKey(votersKeyPairs.First().PublicKey)
                });
                details.Details.Count.ShouldBe(candidatesKeyPairs.Count);
            }

            return candidatesKeyPairs;
        }

        [Fact]
        public async Task ElectionContract_ChangeVotingTarget_With_Invalid_Target_Test()
        {
            const long amount = 500;
            const int lockTime = 100 * 60 * 60 * 24;
            var validCandidate = ValidationDataCenterKeyPairs.First();
            var validCandidatePublicKeyStr = validCandidate.PublicKey.ToHex();
            var invalidCandidate = ValidationDataCenterKeyPairs.Last();
            var invalidCandidatePublicKeyStr = invalidCandidate.PublicKey.ToHex();
            await AnnounceElectionAsync(validCandidate);
            var voterKeyPair = VoterKeyPairs[0];
            var electionStub = GetElectionContractTester(voterKeyPair);
            var voteResult = await VoteToCandidate(voterKeyPair, validCandidate.PublicKey.ToHex(), lockTime, amount);
            voteResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var candidateVote = await electionStub.GetCandidateVote.CallAsync(new StringValue
            {
                Value = validCandidatePublicKeyStr       
            });
            candidateVote.ObtainedActiveVotingRecordIds.Count.ShouldBe(1);
            candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(amount);
            var voteId = candidateVote.ObtainedActiveVotingRecordIds[0];
            
            // change to a candidate that does not exist
            var changeVoteOptionRet = await electionStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
            {
                VoteId = voteId,
                CandidatePubkey = invalidCandidatePublicKeyStr,
            });
            changeVoteOptionRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            changeVoteOptionRet.TransactionResult.Error.ShouldContain("Candidate not found");
            
            // change to a candidate that quits election
            await AnnounceElectionAsync(invalidCandidate);
            await QuitElectionAsync(invalidCandidate);
            changeVoteOptionRet = await electionStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
            {
                VoteId = voteId,
                CandidatePubkey = invalidCandidatePublicKeyStr,
            });
            changeVoteOptionRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            changeVoteOptionRet.TransactionResult.Error.ShouldContain("Candidate quited election");
        }
        
        [Fact]
        public async Task ElectionContract_Vote_DataCenter_Amount_Test()
        {
            const long amount = 500;
            const int lockTime = 100 * 60 * 60 * 24;

            var candidatesKeyPairs = await ElectionContract_AnnounceElection_Test();
            var candidateKeyPair = candidatesKeyPairs.First();
            var voterKeyPair = VoterKeyPairs.First();
            var candidateStringKey = candidateKeyPair.PublicKey.ToHex();
            var firstVoteRet = await VoteToCandidate(voterKeyPair,candidateStringKey, lockTime, amount);
            firstVoteRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var dataCenter = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenter.DataCenters.ContainsKey(candidateStringKey).ShouldBeTrue();
            dataCenter.DataCenters[candidateStringKey].ShouldBe(amount);
            var secondVoteRet = await VoteToCandidate(voterKeyPair,candidateStringKey, lockTime, amount);
            secondVoteRet.Status.ShouldBe(TransactionResultStatus.Mined);
            dataCenter = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenter.DataCenters[candidateStringKey].ShouldBe(amount * 2);
        }
        
        [Fact]
        public async Task ElectionContract_ChangeVotingTarget()
        {
            var candidatesKeyPairs = await ElectionContract_Vote_Test();
            var voterKeyPair = VoterKeyPairs[0];

            var electionStub = GetElectionContractTester(voterKeyPair);

            var electorVote = await electionStub.GetElectorVoteWithRecords.CallAsync(new StringValue
            {
                Value = voterKeyPair.PublicKey.ToHex()
            });

            var voteInformation = electorVote.ActiveVotingRecords[0];

            var oldTarget = voteInformation.Candidate;
            var newTarget = candidatesKeyPairs.Last().PublicKey.ToHex();
            Hash voteId;

            // Check old target
            {
                var candidateVote = await electionStub.GetCandidateVote.CallAsync(new StringValue
                {
                    Value = oldTarget
                });
                candidateVote.ObtainedActiveVotingRecordIds.Count.ShouldBe(2);
                candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(1000);
                voteId = candidateVote.ObtainedActiveVotingRecordIds[0];
            }

            // Check new target
            {
                var candidateVote = await electionStub.GetCandidateVote.CallAsync(new StringValue
                {
                    Value = newTarget
                });
                candidateVote.ObtainedActiveVotingRecordIds.Count.ShouldBe(2);
                candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(1000);
            }

            var transactionResult = (await electionStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
            {
                CandidatePubkey = newTarget,
                VoteId = voteId
            })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check old target
            {
                var candidateVote = await electionStub.GetCandidateVote.CallAsync(new StringValue
                {
                    Value = oldTarget
                });
                candidateVote.ObtainedActiveVotingRecordIds.Count.ShouldBe(1);
                candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(500);
            }

            // Check new target
            {
                var candidateVote = await electionStub.GetCandidateVote.CallAsync(new StringValue
                {
                    Value = newTarget
                });
                candidateVote.ObtainedActiveVotingRecordIds.Count.ShouldBe(3);
                candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(1500);
            }
        }

        [Fact]
        public async Task ElectionContract_ChangeVoting_To_NewTarget()
        {
            var voter = VoterKeyPairs.First();
            var voteAmount = 100;
            var lockTime = 120 * 60 * 60 * 24;
            var candidateKeyPairs = ValidationDataCenterKeyPairs.Take(2).ToList();
            var firstCandidate = candidateKeyPairs[0];
            var secondCandidate = candidateKeyPairs[1];
            await AnnounceElectionAsync(firstCandidate);
            await AnnounceElectionAsync(secondCandidate);
            await VoteToCandidate(voter,firstCandidate.PublicKey.ToHex(), lockTime, voteAmount);
            var electionStub = GetElectionContractTester(voter);
            var beforeChangeVote = await electionStub.GetCandidateVote.CallAsync(new StringValue
            {
                Value = secondCandidate.PublicKey.ToHex()
            });
            beforeChangeVote.ObtainedActiveVotingRecords.Count.ShouldBe(0);
            var candidateVote = await electionStub.GetCandidateVote.CallAsync(new StringValue
            {
                Value = firstCandidate.PublicKey.ToHex()
            });
            candidateVote.ObtainedActiveVotingRecordIds.Count.ShouldBe(1);
            candidateVote.ObtainedActiveVotedVotesAmount.ShouldBe(voteAmount);
            var voteId = candidateVote.ObtainedActiveVotingRecordIds[0];
            var changeRet = await electionStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
            {
                CandidatePubkey = secondCandidate.PublicKey.ToHex(),
                VoteId = voteId
            });
            changeRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterChangeVote = await electionStub.GetCandidateVote.CallAsync(new StringValue
            {
                Value = secondCandidate.PublicKey.ToHex()
            });
            afterChangeVote.ObtainedActiveVotingRecordIds.Count.ShouldBe(1);
            afterChangeVote.ObtainedActiveVotedVotesAmount.ShouldBe(voteAmount);
        }

        [Fact]
        public async Task ElectionContract_Withdraw_Test()
        {
            const int amount = 1000;
            const int lockTime = 120 * 60 * 60 * 24;

            var candidateKeyPair = ValidationDataCenterKeyPairs[0];
            await AnnounceElectionAsync(candidateKeyPair);

            var voterKeyPair = VoterKeyPairs[0];
            var candidateStringKey = candidateKeyPair.PublicKey.ToHex();
            // Vote
            var transactionResult =
                await VoteToCandidate(voterKeyPair, candidateStringKey, lockTime, amount);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var voteId =
                (await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                    {Value = voterKeyPair.PublicKey.ToHex()})).ActiveVotingRecordIds.First();

            voteId.ShouldBe(Hash.Parser.ParseFrom(transactionResult.ReturnValue));
            var candidateInfoBeforeWithdraw = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
            {
                Value = candidateStringKey
            });
            await NextTerm(InitialCoreDataCenterKeyPairs[0]);

            BlockTimeProvider.SetBlockTime(StartTimestamp.AddSeconds(lockTime + 1));

            
            // Withdraw
            {
                var executionResult = await WithdrawVotes(voterKeyPair, voteId);
                executionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            var candidateInfoAfterWithdraw = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
            {
                Value = candidateStringKey
            });
            candidateInfoBeforeWithdraw.AllObtainedVotedVotesAmount.ShouldBe(candidateInfoAfterWithdraw.AllObtainedVotedVotesAmount);
            candidateInfoBeforeWithdraw.ObtainedActiveVotedVotesAmount.Sub(candidateInfoAfterWithdraw.ObtainedActiveVotedVotesAmount).ShouldBe(amount);

            candidateInfoBeforeWithdraw.ObtainedActiveVotingRecordIds.Contains(voteId).ShouldBeTrue();
            candidateInfoAfterWithdraw.ObtainedActiveVotingRecordIds.Contains(voteId).ShouldBeFalse();
            candidateInfoAfterWithdraw.ObtainedWithdrawnVotingRecordIds.Contains(voteId).ShouldBeTrue();
            
            //check candidate records
            {
                var candidateVote = await ElectionContractStub.GetCandidateVoteWithAllRecords.CallAsync(new StringValue
                {
                    Value = candidateStringKey
                });
                candidateVote.ObtainedWithdrawnVotesRecords.Select(o=>o.VoteId).ShouldContain(voteId);
            }

            // Profit
            var voter = GetProfitContractTester(voterKeyPair);
            var claimResult = await voter.ClaimProfits.SendAsync(new ClaimProfitsInput
            {
                SchemeId = ProfitItemsIds[ProfitType.CitizenWelfare],
            });
            claimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check VOTE token balance.
            {
                var balance = await GetVoteTokenBalance(voterKeyPair.PublicKey);
                balance.ShouldBe(0);
            }
        }

        [Fact]
        public async Task ElectionContract_GetCandidates_Test()
        {
            var announcedFullNodesKeyPairs = await ElectionContract_AnnounceElection_Test();
            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            announcedFullNodesKeyPairs.Count.ShouldBe(candidates.Value.Count);
            foreach (var keyPair in announcedFullNodesKeyPairs)
            {
                candidates.Value.ShouldContain(ByteString.CopyFrom(keyPair.PublicKey));
            }
        }

        [Fact]
        public async Task ElectionContract_GetCandidateInformation_Test()
        {
            const int roundCount = 5;

            var minerKeyPair = ValidationDataCenterKeyPairs[0];

            await ElectionContract_GetVictories_ValidCandidatesEnough_Test();

            await ProduceBlocks(BootMinerKeyPair, 1, true);

            await ProduceBlocks(minerKeyPair, roundCount, true);

            var information = await ElectionContractStub.GetCandidateInformation.CallAsync(new StringValue
            {
                Value = minerKeyPair.PublicKey.ToHex()
            });

            information.Pubkey.ShouldBe(minerKeyPair.PublicKey.ToHex());
        }

        [Fact]
        public async Task ConsensusContract_GetPreviousMinerList()
        {
            await ElectionContract_GetVictories_ValidCandidatesEnough_Test();
            await ProduceBlocks(BootMinerKeyPair, 1, true);
            var previousMiners = await AEDPoSContractStub.GetPreviousMinerList.CallAsync(new Empty());
            var previousMinersPubkey = previousMiners.Pubkeys.Select(o => o.ToHex()).OrderBy(o=>o).ToList();
            var initialMinersPubkey = InitialCoreDataCenterKeyPairs.Select(o => o.PublicKey.ToHex()).OrderBy(o=>o).ToList();
            previousMinersPubkey.ShouldBe(initialMinersPubkey);
        }

        [Fact]
        public async Task Election_VoteWeightInterestSetting_With_Invalid_Input_Test()
        {
            // argument <= 0
            {
                var newSetting = new VoteWeightInterestList
                {
                    VoteWeightInterestInfos =
                    {
                        new VoteWeightInterest
                        {
                            Capital = 0,
                            Interest = 4,
                            Day = 0
                        }
                    }
                };
                var settingRet = await ExecuteProposalForParliamentTransactionWithException(BootMinerAddress, ElectionContractAddress,
                    nameof(ElectionContractStub.SetVoteWeightInterest), newSetting);
                settingRet.Status.ShouldBe(TransactionResultStatus.Failed);
                settingRet.Error.ShouldContain("invalid input");
            }
            
            // interest count == 0
            {
                var newSetting = new VoteWeightInterestList();
                var settingRet = await ExecuteProposalForParliamentTransactionWithException(BootMinerAddress, ElectionContractAddress,
                    nameof(ElectionContractStub.SetVoteWeightInterest), newSetting);
                settingRet.Status.ShouldBe(TransactionResultStatus.Failed);
                settingRet.Error.ShouldContain("invalid input");
            }
            
            // repeat day
            {
                var newSetting = new VoteWeightInterestList
                {
                    VoteWeightInterestInfos =
                    {
                        new VoteWeightInterest
                        {
                            Capital = 1,
                            Interest = 2,
                            Day = 3
                        },
                        new VoteWeightInterest
                        {
                            Capital = 1,
                            Interest = 2,
                            Day = 3
                        }
                    }
                };
                var settingRet = await ExecuteProposalForParliamentTransactionWithException(BootMinerAddress, ElectionContractAddress,
                    nameof(ElectionContractStub.SetVoteWeightInterest), newSetting);
                settingRet.Status.ShouldBe(TransactionResultStatus.Failed);
                settingRet.Error.ShouldContain("repeat day");
            }
        }
        
        [Fact]
        public async Task Election_VoteWeightInterestSetting_Without_Authority_Test()
        {
            var defaultSetting = await ElectionContractStub.GetVoteWeightSetting.CallAsync(
                new Empty());
            defaultSetting.VoteWeightInterestInfos.Count.ShouldBe(3);
            defaultSetting.VoteWeightInterestInfos[0].Capital = 13200;
            defaultSetting.VoteWeightInterestInfos[0].Day = 50;

            var setWithoutAuthRet = await ElectionContractStub.SetVoteWeightInterest.SendAsync(defaultSetting);
            setWithoutAuthRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            setWithoutAuthRet.TransactionResult.Error.ShouldContain("No permission.");
        }

        [Fact]
        public async Task Election_VoteWeightInterestSetting_Test()
        {
            var defaultSetting = await ElectionContractStub.GetVoteWeightSetting.CallAsync(
                new Empty());
            defaultSetting.VoteWeightInterestInfos.Count.ShouldBe(3);
            defaultSetting.VoteWeightInterestInfos[0].Capital = 13200;
            defaultSetting.VoteWeightInterestInfos[0].Day = 50;

            await ExecuteProposalForParliamentTransaction(BootMinerAddress, ElectionContractAddress,
                nameof(ElectionContractStub.SetVoteWeightInterest), defaultSetting);
            
            defaultSetting = await ElectionContractStub.GetVoteWeightSetting.CallAsync(
                new Empty());
            defaultSetting.VoteWeightInterestInfos[0].Capital.ShouldBe(13200);
            defaultSetting.VoteWeightInterestInfos[0].Day.ShouldBe(50);
        }

        [Fact]
        public async Task Election_ChangeVoteWeightInterestController_Without_Authority_Test()
        {
            var newAuthority = new AuthorityInfo
            {
                OwnerAddress = new Address(),
                ContractAddress = ParliamentContractAddress
            };
            var changeControllerRet =
                await ElectionContractStub.ChangeVoteWeightInterestController.SendAsync(newAuthority);
            changeControllerRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            changeControllerRet.TransactionResult.Error.ShouldContain("No Permission");
        }
        
        [Fact]
        public async Task Election_ChangeVoteWeightInterestController_With_Invalid_Input_Test()
        {
            var newAuthority = new AuthorityInfo
            {
                OwnerAddress = TokenHolderContractAddress,
                ContractAddress = ParliamentContractAddress
            };
            var changeControllerRet = await ExecuteProposalForParliamentTransactionWithException(BootMinerAddress, ElectionContractAddress,
                nameof(ElectionContractStub.ChangeVoteWeightInterestController), newAuthority);
            changeControllerRet.Status.ShouldBe(TransactionResultStatus.Failed);
            changeControllerRet.Error.ShouldContain("Invalid authority input.");
        }

        [Fact]
        public async Task Election_ChangeVoteWeightInterestController_Test()
        {
            var defaultSetting = await ElectionContractStub.GetVoteWeightSetting.CallAsync(
                new Empty());
            defaultSetting.VoteWeightInterestInfos[0].Capital = 13200;
            var newParliament = new CreateOrganizationInput
            {
                ProposerAuthorityRequired = false,
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ParliamentMemberProposingAllowed = false
            };
            var buildNewParliamentRet =
                (await ParliamentContractStub.CreateOrganization.SendAsync(newParliament)).TransactionResult;
            buildNewParliamentRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var newParliamentAddress =  new Address();
            newParliamentAddress.MergeFrom(buildNewParliamentRet.ReturnValue);
            var newAuthority = new AuthorityInfo
            {
                OwnerAddress = newParliamentAddress,
                ContractAddress = ParliamentContractAddress
            };
            await ExecuteProposalForParliamentTransaction(BootMinerAddress, ElectionContractAddress,
                nameof(ElectionContractStub.ChangeVoteWeightInterestController), newAuthority);

            var getNewAuthority = await ElectionContractStub.GetVoteWeightInterestController.CallAsync(new Empty());
            getNewAuthority.ContractAddress.ShouldBe(newAuthority.ContractAddress);
            getNewAuthority.OwnerAddress.ShouldBe(newAuthority.OwnerAddress);
        }
        
        [Fact]
        public async Task Election_SetVoteWeightProportion_Without_Authority_Test()
        {
            var newSetting = new VoteWeightProportion
            {
                TimeProportion = 3,
                AmountProportion = 3
            };
            var settingRet = await ElectionContractStub.SetVoteWeightProportion.SendAsync(newSetting);
            settingRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            settingRet.TransactionResult.Error.ShouldContain("No permission");
        }
        
        [Fact]
        public async Task Election_SetVoteWeightProportion_With_Invalid_Input_Test()
        {
            var newSetting = new VoteWeightProportion
            {
                TimeProportion = 0,
                AmountProportion = 3
            };
            var settingRet =  await ExecuteProposalForParliamentTransactionWithException(BootMinerAddress, ElectionContractAddress,
                nameof(ElectionContractStub.SetVoteWeightProportion), newSetting);
            settingRet.Status.ShouldBe(TransactionResultStatus.Failed);
            settingRet.Error.ShouldContain("invalid input");
        }
        
        [Fact]
        public async Task Election_SetVoteWeightProportion_Test()
        {
            var defaultSetting = await ElectionContractStub.GetVoteWeightProportion.CallAsync(
                new Empty());
            defaultSetting.TimeProportion.ShouldBe(2);
            defaultSetting.AmountProportion.ShouldBe(1);
            defaultSetting = new VoteWeightProportion
            {
                TimeProportion = 3,
                AmountProportion = 3
            };
            await ExecuteProposalForParliamentTransaction(BootMinerAddress, ElectionContractAddress,
                nameof(ElectionContractStub.SetVoteWeightProportion), defaultSetting);
            
            defaultSetting = await ElectionContractStub.GetVoteWeightProportion.CallAsync(
                new Empty());
            defaultSetting.TimeProportion.ShouldBe(3);
            defaultSetting.AmountProportion.ShouldBe(3);
        }
        
        #region test about adjust data center 
        [Fact]
        public async Task ElectionContract_Vote_DataCenter_Replace_Test()
        {
            var voter = VoterKeyPairs.First();
            var voteAmount = 100;
            var span = 100;
            var lockTime = 120 * 60 * 60 * 24;
            var fullCount = 5.Mul(5);
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(fullCount))
            {
                await AnnounceElectionAsync(keyPair);
                await VoteToCandidate(voter,keyPair.PublicKey.ToHex(), lockTime, voteAmount);
                voteAmount = voteAmount.Add(span);
            }

            var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterList.DataCenters.Count.ShouldBe(fullCount);
            var minimumCandidate = dataCenterList.DataCenters.First();
            var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
            await AnnounceElectionAsync(newCandidate);
            var voteToCandidateRet = await VoteToCandidate(voter, newCandidate.PublicKey.ToHex(), lockTime, voteAmount);
            voteToCandidateRet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var newDataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            newDataCenterList.DataCenters.ContainsKey(minimumCandidate.Key).ShouldBeFalse();
            newDataCenterList.DataCenters.ContainsKey(newCandidate.PublicKey.ToHex()).ShouldBeTrue();
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            var profitDetailOfOldOne = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(minimumCandidate.Key))
            });
            profitDetailOfOldOne.Details.Count.ShouldBe(1);
            profitDetailOfOldOne.Details[0].EndPeriod.ShouldBe(0);
            var profitDetailOfNewOne = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(newCandidate.PublicKey)
            });
            profitDetailOfNewOne.Details.Count.ShouldBe(1);
        }
        
        [Fact]
        public async Task ElectionContract_Withdraw_DataCenter_Replace_Test()
        {
            var voter = VoterKeyPairs.First();
            var voteAmount = 100;
            var span = 100;
            var lockTime = 120 * 60 * 60 * 24;
            var fullCount = 5.Mul(5);
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(fullCount))
            {
                await AnnounceElectionAsync(keyPair);
                await VoteToCandidate(voter,keyPair.PublicKey.ToHex(), lockTime, voteAmount);
                voteAmount = voteAmount.Add(span);
            }

            var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            var minimumCandidate = dataCenterList.DataCenters.First();
            // vote a new candidate that should be added to the date center, and the minimum one in data center should be removed.
            var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
            await AnnounceElectionAsync(newCandidate);
            var voter2 = VoterKeyPairs.Skip(1).First();
            var voteToCandidateRet = await VoteToCandidate(voter2, newCandidate.PublicKey.ToHex(), lockTime, voteAmount);
            voteToCandidateRet.Status.ShouldBe(TransactionResultStatus.Mined);

            // after withdraw, the new candidate should be removed
            BlockTimeProvider.SetBlockTime(StartTimestamp.AddSeconds(lockTime + 1));
            var electionVoteItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
            var voteIdOfVoter2 = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
            {
                Voter = Address.FromPublicKey(voter2.PublicKey),
                VotingItemId = electionVoteItemId
            });
            voteIdOfVoter2.ActiveVotes.Count.ShouldBe(1);
            var withdrawVotesRet = await WithdrawVotes(voter2, voteIdOfVoter2.ActiveVotes[0]);
            withdrawVotesRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var dataCenterListAfterWithDraw = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterListAfterWithDraw.DataCenters.ContainsKey(minimumCandidate.Key).ShouldBeTrue();
            dataCenterListAfterWithDraw.DataCenters.ContainsKey(newCandidate.PublicKey.ToHex()).ShouldBeFalse();
            
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            var profitDetail = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(minimumCandidate.Key))
            });
            profitDetail.Details[0].EndPeriod.ShouldBe(0);
            profitDetail.Details[1].EndPeriod.ShouldBe(long.MaxValue);
            var profitDetailOfTheWithdraw = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(newCandidate.PublicKey)
            });
            profitDetailOfTheWithdraw.Details.Count.ShouldBe(1);
        }

        [Fact]
        public async Task ElectionContract_QuitElection_DataCenter_Replace_Test()
        {
            var voter = VoterKeyPairs.First();
            var voteAmount = 100;
            var span = 100;
            var lockTime = 120 * 60 * 60 * 24;
            var fullCount = 5.Mul(5);
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(fullCount))
            {
                await AnnounceElectionAsync(keyPair);
                await VoteToCandidate(voter,keyPair.PublicKey.ToHex(), lockTime, voteAmount);
                voteAmount = voteAmount.Add(span);
            }
            var maximumVoteAmountCandidate = ValidationDataCenterKeyPairs[fullCount - 1];
            var newCandidate1 = ValidationDataCenterCandidateKeyPairs.First();
            var newCandidate2 = ValidationDataCenterCandidateKeyPairs.Skip(1).First();
            await AnnounceElectionAsync(newCandidate1);
            await AnnounceElectionAsync(newCandidate2);
            await VoteToCandidate(voter,newCandidate1.PublicKey.ToHex(), lockTime, span.Div(2));
            await VoteToCandidate(voter,newCandidate2.PublicKey.ToHex(), lockTime, span.Div(3));
            await QuitElectionAsync(maximumVoteAmountCandidate);
            var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterList.DataCenters.Count.ShouldBe(fullCount);
            dataCenterList.DataCenters.ContainsKey(newCandidate1.PublicKey.ToHex()).ShouldBeTrue();
            dataCenterList.DataCenters.ContainsKey(maximumVoteAmountCandidate.PublicKey.ToHex()).ShouldBeFalse();
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            var profitDetailOfNewCandidate = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(newCandidate1.PublicKey)
            });
            profitDetailOfNewCandidate.Details.Count.ShouldBe(1);
            var profitDetailOfMaximumVoteAmountCandidate = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(maximumVoteAmountCandidate.PublicKey)
            });
            profitDetailOfMaximumVoteAmountCandidate.Details.Count.ShouldBe(1);
            profitDetailOfMaximumVoteAmountCandidate.Details[0].EndPeriod.ShouldBe(0);
            
            var candidateToQuit1 = ValidationDataCenterKeyPairs[0];
            var candidateToQuit2 = ValidationDataCenterKeyPairs[1];
            await QuitElectionAsync(candidateToQuit1);
            await QuitElectionAsync(candidateToQuit2);
            dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterList.DataCenters.Count.ShouldBe(fullCount - 1);
            var profitDetailOfMinimumCandidate = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(candidateToQuit1.PublicKey)
            });
            profitDetailOfMinimumCandidate.Details.Count.ShouldBe(1);
            profitDetailOfMinimumCandidate.Details[0].EndPeriod.ShouldBe(0);
        }
        
        /// <summary>
        /// changeVoteOption from a candidate A who is in the data center, and it will be replaced by someone out of data center
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ElectionContract_ChangeVoteOption_DataCenter_Replace_Test()
        {
            var voter = VoterKeyPairs.First();
            var voteAmount = 100;
            var span = 100;
            var lockTime = 120 * 60 * 60 * 24;
            var fullCount = 5.Mul(5);
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(fullCount))
            {
                await AnnounceElectionAsync(keyPair);
                await VoteToCandidate(voter,keyPair.PublicKey.ToHex(), lockTime, voteAmount);
                voteAmount = voteAmount.Add(span);
            }

            var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            var minimumCandidate = dataCenterList.DataCenters.First();
            // vote a new candidate that should be added to the date center, and the minimum one in data center should be removed.
            var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
            await AnnounceElectionAsync(newCandidate);
            var voter2 = VoterKeyPairs.Skip(1).First();
            var voteToCandidateRet = await VoteToCandidate(voter2, newCandidate.PublicKey.ToHex(), lockTime, voteAmount);
            voteToCandidateRet.Status.ShouldBe(TransactionResultStatus.Mined);

            // after change option, the minimum candidate amount add to 2700, it should be in data center
            var electionVoteItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
            var voteIdOfVoter2 = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
            {
                Voter = Address.FromPublicKey(voter2.PublicKey),
                VotingItemId = electionVoteItemId
            });
            voteIdOfVoter2.ActiveVotes.Count.ShouldBe(1);
            // await change
            var changeOptionRet = await ChangeVoteOption(voter2, voteIdOfVoter2.ActiveVotes[0], minimumCandidate.Key);
            changeOptionRet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var dataCenterListAfterChangeOption = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterListAfterChangeOption.DataCenters.ContainsKey(minimumCandidate.Key).ShouldBeTrue();
            dataCenterListAfterChangeOption.DataCenters.ContainsKey(newCandidate.PublicKey.ToHex()).ShouldBeFalse();
            
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            var profitDetailOfTheIn = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(minimumCandidate.Key))
            });
            profitDetailOfTheIn.Details.Count.ShouldBe(2);
            profitDetailOfTheIn.Details[0].EndPeriod.ShouldBe(0);
            profitDetailOfTheIn.Details[1].EndPeriod.ShouldBe(long.MaxValue);
            var profitDetailOfTheOut = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(newCandidate.PublicKey)
            });
            profitDetailOfTheOut.Details.Count.ShouldBe(1);
            profitDetailOfTheOut.Details[0].EndPeriod.ShouldBe(0);
        }

        /// <summary>
        /// changeVoteOption from a candidate A who is in the data center, and it has enough vote to stay in the date center
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ElectionContract_ChangeVoteOption_DataCenter_Remain_Unchanged_Test()
        {
            var voter = VoterKeyPairs.First();
            var voteAmount = 100;
            var span = 100;
            var lockTime = 120 * 60 * 60 * 24;
            var fullCount = 5.Mul(5);
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(fullCount))
            {
                await AnnounceElectionAsync(keyPair);
                await VoteToCandidate(voter,keyPair.PublicKey.ToHex(), lockTime, voteAmount);
                voteAmount = voteAmount.Add(span);
            }
            var maximumVoteCandidate = ValidationDataCenterKeyPairs[fullCount - 1];
            var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
            await AnnounceElectionAsync(newCandidate);
            var voter2 = VoterKeyPairs.Skip(1).First();
            var voteAmount2 = 100;
            var voteToCandidateRet = await VoteToCandidate(voter2, maximumVoteCandidate.PublicKey.ToHex(), lockTime, voteAmount2);
            voteToCandidateRet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var electionVoteItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
            var voteIdOfVoter2 = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
            {
                Voter = Address.FromPublicKey(voter2.PublicKey),
                VotingItemId = electionVoteItemId
            });
            voteIdOfVoter2.ActiveVotes.Count.ShouldBe(1);
            var dataCenterListBeforeChangeOption = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            // await change
            var changeOptionRet = await ChangeVoteOption(voter2, voteIdOfVoter2.ActiveVotes[0], newCandidate.PublicKey.ToHex());
            changeOptionRet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var dataCenterListAfterChangeOption = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterListBeforeChangeOption.DataCenters.Keys.ShouldNotContain(newCandidate.PublicKey.ToHex());
            dataCenterListAfterChangeOption.DataCenters.Keys.ShouldNotContain(newCandidate.PublicKey.ToHex());
        }
        
        /// <summary>
        /// changeVoteOption to another candidate A, and A's vote amount is greater than someone in data center
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ElectionContract_ChangeVoteOption_New_Candidate_Replace_DataCenter_Test()
        {
            var voter = VoterKeyPairs.First();
            var voteAmount = 100;
            var span = 100;
            var lockTime = 120 * 60 * 60 * 24;
            var fullCount = 5.Mul(5);
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(fullCount))
            {
                await AnnounceElectionAsync(keyPair);
                await VoteToCandidate(voter,keyPair.PublicKey.ToHex(), lockTime, voteAmount);
                voteAmount = voteAmount.Add(span);
            }
            var minimumVoteCandidate= ValidationDataCenterKeyPairs[0];
            var maximumVoteCandidate = ValidationDataCenterKeyPairs[fullCount - 1];
            var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
            await AnnounceElectionAsync(newCandidate);
            var voter2 = VoterKeyPairs.Skip(1).First();
            var voteToCandidateRet = await VoteToCandidate(voter2, maximumVoteCandidate.PublicKey.ToHex(), lockTime, voteAmount);
            voteToCandidateRet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var electionVoteItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
            var voteIdOfVoter2 = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
            {
                Voter = Address.FromPublicKey(voter2.PublicKey),
                VotingItemId = electionVoteItemId
            });
            voteIdOfVoter2.ActiveVotes.Count.ShouldBe(1);
            var dataCenterListBeforeChangeOption = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            // await change
            var changeOptionRet = await ChangeVoteOption(voter2, voteIdOfVoter2.ActiveVotes[0], newCandidate.PublicKey.ToHex());
            changeOptionRet.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var dataCenterListAfterChangeOption = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterListBeforeChangeOption.DataCenters.Keys.ShouldNotContain(newCandidate.PublicKey.ToHex());
            dataCenterListAfterChangeOption.DataCenters.Keys.ShouldContain(newCandidate.PublicKey.ToHex());
            dataCenterListAfterChangeOption.DataCenters.Keys.ShouldNotContain(minimumVoteCandidate.PublicKey.ToHex());
            
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            var profitDetailOfTheIn = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(newCandidate.PublicKey)
            });
            profitDetailOfTheIn.Details.Count.ShouldBe(1);
            
            var profitDetailOfTheOut = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(minimumVoteCandidate.PublicKey)
            });
            profitDetailOfTheOut.Details.Count.ShouldBe(1);
            profitDetailOfTheOut.Details[0].EndPeriod.ShouldBe(0);
        }
        
        [Fact]
        public async Task ElectionContract_UpdateMinerCount_ReduceBp_Test()
        {
            var voter = VoterKeyPairs.First();
            var voteAmount = 100;
            var span = 100;
            var lockTime = 120 * 60 * 60 * 24;
            var fullCount = 5.Mul(5);
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(fullCount))
            {
                await AnnounceElectionAsync(keyPair);
                await VoteToCandidate(voter, keyPair.PublicKey.ToHex(), lockTime, voteAmount);
                voteAmount = voteAmount.Add(span);
            }
            var minerCount = 3;
            await NextRound(InitialCoreDataCenterKeyPairs[0]);
            var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterList.DataCenters.Count.ShouldBe(fullCount);
            var diffCount = fullCount.Sub(minerCount.Mul(5));
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(diffCount))
            {
                var profitDetail = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = subsidy,
                    Beneficiary = Address.FromPublicKey(keyPair.PublicKey)
                });
                profitDetail.Details[0].EndPeriod.ShouldNotBe(0);
                profitDetail.Details.Count.ShouldBe(1);
            }
            await ResetMinerCount(minerCount);
            await NextTerm(InitialCoreDataCenterKeyPairs[0]);
            var newMinerCount = await ElectionContractStub.GetMinersCount.CallAsync(new Empty());
            newMinerCount.Value.ShouldBe(minerCount);
            var dataCenterListAfterReduceBp =
                await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());

            dataCenterList.DataCenters.Count.Sub(dataCenterListAfterReduceBp.DataCenters.Count).ShouldBe(diffCount);
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(diffCount))
            {
                dataCenterListAfterReduceBp.DataCenters.ContainsKey(keyPair.PublicKey.ToHex()).ShouldBeFalse();
                var profitDetail = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
                {
                    SchemeId = subsidy,
                    Beneficiary = Address.FromPublicKey(keyPair.PublicKey)
                });
                profitDetail.Details[0].EndPeriod.ShouldBe(0);
            }
        }

        private async Task ResetMinerCount(int count)
        {
            var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposalMethodName = nameof(AEDPoSContractStub.SetMaximumMinersCount);
            var param = new Int32Value
            {
                Value = count
            };
            var proposalId = await CreateProposalAsync(ConsensusContractAddress,
                defaultParliament, proposalMethodName, param);
            await ApproveWithMinersAsync(proposalId);
            await ParliamentContractStub.Release.SendAsync(proposalId);
        }
        
        #endregion
    }
}