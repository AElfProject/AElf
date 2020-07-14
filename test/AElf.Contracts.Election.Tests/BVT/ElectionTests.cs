using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core.Extension;
using Google.Protobuf.Collections;

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
            balanceBeforeAnnouncing.ShouldBe(balanceAfterAnnouncing + ElectionContractConstants.LockTokenForElection);
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
        public async Task<List<ECKeyPair>> ElectionContract_AnnounceElection_Test()
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

        #pragma warning disable xUnit1013
        public async Task ElectionContract_QuiteElection_Test()
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
            // profitDetail.Details.Count.ShouldBe(0);
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
        public async Task ElectionContract_Withdraw_Test()
        {
            const int amount = 1000;
            const int lockTime = 120 * 60 * 60 * 24;

            var candidateKeyPair = ValidationDataCenterKeyPairs[0];
            await AnnounceElectionAsync(candidateKeyPair);

            var voterKeyPair = VoterKeyPairs[0];
            var beforeBalance = await GetNativeTokenBalance(voterKeyPair.PublicKey);

            // Vote
            var transactionResult =
                await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), lockTime, amount);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var voteId =
                (await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                    {Value = voterKeyPair.PublicKey.ToHex()})).ActiveVotingRecordIds.First();

            voteId.ShouldBe(Hash.Parser.ParseFrom(transactionResult.ReturnValue));
            await NextTerm(InitialCoreDataCenterKeyPairs[0]);

            BlockTimeProvider.SetBlockTime(StartTimestamp.AddSeconds(lockTime + 1));

            
            // Withdraw
            {
                var executionResult = await WithdrawVotes(voterKeyPair, voteId);
                executionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            //check candidate records
            {
                var candidateVote = await ElectionContractStub.GetCandidateVoteWithAllRecords.CallAsync(new StringValue
                {
                    Value = candidateKeyPair.PublicKey.ToHex()
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
        public async Task ElectionContract_MarkCandidateAsEvilNode_Test()
        {
            await ElectionContract_AnnounceElection_Test();

            var pubkey = ValidationDataCenterKeyPairs.First().PublicKey.ToHex();
            var transactionResult = (await ElectionContractStub.UpdateCandidateInformation.SendAsync(
                new UpdateCandidateInformationInput
                {
                    IsEvilNode = true,
                    Pubkey = pubkey,
                    RecentlyProducedBlocks = 10,
                    RecentlyMissedTimeSlots = 100
                })).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed); // No permission.
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
                var settingRet = await ExecuteProposalTransactionWithTransactionResult(BootMinerAddress, ElectionContractAddress,
                    nameof(ElectionContractStub.SetVoteWeightInterest), newSetting);
                settingRet.Status.ShouldBe(TransactionResultStatus.Failed);
                settingRet.Error.ShouldContain("invalid input");
            }
            
            // interest count == 0
            {
                var newSetting = new VoteWeightInterestList();
                var settingRet = await ExecuteProposalTransactionWithTransactionResult(BootMinerAddress, ElectionContractAddress,
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
                var settingRet = await ExecuteProposalTransactionWithTransactionResult(BootMinerAddress, ElectionContractAddress,
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

            await ExecuteProposalTransaction(BootMinerAddress, ElectionContractAddress,
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
            var changeControllerRet = await ExecuteProposalTransactionWithTransactionResult(BootMinerAddress, ElectionContractAddress,
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
            await ExecuteProposalTransaction(BootMinerAddress, ElectionContractAddress,
                nameof(ElectionContractStub.ChangeVoteWeightInterestController), newAuthority);

            await ExecuteProposalTransactionWithNewParliament(BootMinerAddress, ElectionContractAddress,
                nameof(ElectionContractStub.SetVoteWeightInterest), defaultSetting, newParliamentAddress);
            defaultSetting = await ElectionContractStub.GetVoteWeightSetting.CallAsync(
                new Empty());
            defaultSetting.VoteWeightInterestInfos[0].Capital.ShouldBe(13200);
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
            var settingRet =  await ExecuteProposalTransactionWithTransactionResult(BootMinerAddress, ElectionContractAddress,
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
            await ExecuteProposalTransaction(BootMinerAddress, ElectionContractAddress,
                nameof(ElectionContractStub.SetVoteWeightProportion), defaultSetting);
            
            defaultSetting = await ElectionContractStub.GetVoteWeightProportion.CallAsync(
                new Empty());
            defaultSetting.TimeProportion.ShouldBe(3);
            defaultSetting.AmountProportion.ShouldBe(3);
        }
    }
}