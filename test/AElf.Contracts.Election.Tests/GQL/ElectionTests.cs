using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests
    {
        [Fact]
        public async Task ElectionContract_InitializeTwice_Test()
        {
            var transactionResult = (await ElectionContractStub.InitialElectionContract.SendAsync(
                new InitialElectionContractInput())).TransactionResult;

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task ElectionContract_RegisterElectionVotingEvent_Register_Twice_Test()
        {
            var registerAgainRet =
                await ElectionContractStub.RegisterElectionVotingEvent.SendAsync(new Empty());
            registerAgainRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            registerAgainRet.TransactionResult.Error.ShouldContain("Already registered.");
        }
        
        [Fact]
        public async Task ElectionContract_SetTreasurySchemeIds_SetTwice_Test()
        {
            var setSchemeIdRet = await ElectionContractStub.SetTreasurySchemeIds.SendAsync(new SetTreasurySchemeIdsInput
            {
                SubsidyHash = HashHelper.ComputeFrom("Subsidy"),
                TreasuryHash = HashHelper.ComputeFrom("Treasury"),
                WelfareHash = HashHelper.ComputeFrom("Welfare"),
                VotesRewardHash = HashHelper.ComputeFrom("VotesReward"),
                ReElectionRewardHash = HashHelper.ComputeFrom("ReElectionReward")
            });
            setSchemeIdRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            setSchemeIdRet.TransactionResult.Error.ShouldContain("Treasury profit ids already set.");
        }


        #region AnnounceElection

        [Fact]
        public async Task ElectionContract_AnnounceElection_TokenNotEnough_Test()
        {
            var candidateKeyPair = VoterKeyPairs[0];
            var balance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(candidateKeyPair.PublicKey),
                Symbol = ElectionContractTestConstants.NativeTokenSymbol
            })).Balance;
            var tokenTester = GetTokenContractTester(candidateKeyPair);
            await tokenTester.Transfer.SendAsync(new TransferInput
            {
                Symbol = ElectionContractTestConstants.NativeTokenSymbol,
                Amount = balance / 2,
                To = Address.FromPublicKey(VoterKeyPairs[1].PublicKey),
                Memo = "transfer token to other"
            });
            
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Insufficient balance").ShouldBeTrue();
        }

        [Fact]
        public async Task ElectionContract_AnnounceElection_Twice_Test()
        {
            var s = Stopwatch.StartNew();
            s.Start();
            var candidateKeyPair = (await ElectionContract_AnnounceElection_Test())[0];
            var transactionResult = await AnnounceElectionAsync(candidateKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("This public key already announced election.");
            s.Stop();
            _testOutputHelper.WriteLine(s.ElapsedMilliseconds.ToString());
        }
        
        [Fact]
        public async Task ElectionContract_AnnounceElection_MinerAnnounce_Test()
        {
            var miner = InitialCoreDataCenterKeyPairs[0];
            var minerAnnounceRet = await AnnounceElectionAsync(miner);
            minerAnnounceRet.Status.ShouldBe(TransactionResultStatus.Failed);
            minerAnnounceRet.Error.ShouldContain("Initial miner cannot announce election.");
        }

        #endregion

        #region QuitElection

        [Fact]
        public async Task ElectionContract_QuitElection_NotCandidate_Test()
        {
            var userKeyPair = Accounts[2].KeyPair;

            var transactionResult = await QuitElectionAsync(userKeyPair);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Sender is not a candidate").ShouldBeTrue();
        }

        #endregion

        #region Vote

        [Fact]
        public async Task ElectionContract_Vote_Failed_Test()
        {
            var candidateKeyPair = ValidationDataCenterKeyPairs[0];
            var voterKeyPair = VoterKeyPairs[0];

            // candidateKeyPair not announced election yet.
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400, 100);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Candidate not found");
            }

            await AnnounceElectionAsync(candidateKeyPair);

            // Voter token not enough
            {
                var voterBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(voterKeyPair.PublicKey),
                    Symbol = "ELF"
                })).Balance;
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400, voterBalance + 10);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("Insufficient balance");
            }

            // Lock time is less than 90 days
            {
                var transactionResult =
                    await VoteToCandidate(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 80 * 86400, 1000);

                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.ShouldContain("lock time");
            }
        }

        #endregion

        #region GetVictories

        [Fact]
        public async Task ElectionContract_GetVictories_NoCandidate_Test()
        {
            // To get previous round information.
            await NextRound(BootMinerKeyPair);

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
            foreach (var initialMiner in InitialCoreDataCenterKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }

        [Fact]
        public async Task ElectionContract_GetVictories_CandidatesNotEnough_Test()
        {
            // To get previous round information.
            await NextRound(BootMinerKeyPair);

            ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1).ToList()
                .ForEach(async kp => await AnnounceElectionAsync(kp));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
            foreach (var initialMiner in InitialCoreDataCenterKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }

        [Fact]
        public async Task ElectionContract_GetVictories_NoValidCandidate_Test()
        {
            await NextRound(BootMinerKeyPair);

            ValidationDataCenterKeyPairs.ForEach(async kp => await AnnounceElectionAsync(kp));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Same as initial miners.
            victories.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
            foreach (var initialMiner in InitialCoreDataCenterKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            {
                victories.ShouldContain(initialMiner);
            }
        }

        [Fact]
        public async Task ElectionContract_ToBecomeValidationDataCenter_Test()
        {
            foreach (var keyPair in ValidationDataCenterKeyPairs.Take(25))
            {
                await AnnounceElectionAsync(keyPair);
            }
            
            //add new candidate and vote into data center
            var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
            await AnnounceElectionAsync(newCandidate);

            var voter = VoterKeyPairs.First();
            await
                VoteToCandidate(voter, newCandidate.PublicKey.ToHex(), 100 * 86400, 200);

            var victories = await ElectionContractStub.GetVictories.CallAsync(new Empty());
            victories.Value.Select(o=>o.ToHex()).ShouldContain(newCandidate.PublicKey.ToHex());
        }
        
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
            var minimumCandidate =
                dataCenterList.DataCenters.Aggregate((curMin, x) => curMin.Value < x.Value ? curMin : x);
            
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
            profitDetailOfOldOne.Details.Count.ShouldBe(0);
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
            var minimumCandidate =
                dataCenterList.DataCenters.Aggregate((curMin, x) => curMin.Value < x.Value ? curMin : x);
            
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
            profitDetail.Details.Count.ShouldBe(1);
            var profitDetailOfTheWithdraw = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(newCandidate.PublicKey)
            });
            profitDetailOfTheWithdraw.Details.Count.ShouldBe(0);
        }
        
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
            var minimumCandidate =
                dataCenterList.DataCenters.Aggregate((curMin, x) => curMin.Value < x.Value ? curMin : x);
            
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
            profitDetailOfTheIn.Details.Count.ShouldBe(1);
            var profitDetailOfTheOut = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(newCandidate.PublicKey)
            });
            profitDetailOfTheOut.Details.Count.ShouldBe(0);
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
            var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
            await AnnounceElectionAsync(newCandidate);
            await VoteToCandidate(voter,newCandidate.PublicKey.ToHex(), lockTime, span.Div(2));
            await QuitElectionAsync(maximumVoteAmountCandidate);
            var dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterList.DataCenters.Count.ShouldBe(fullCount);
            dataCenterList.DataCenters.ContainsKey(newCandidate.PublicKey.ToHex()).ShouldBeTrue();
            dataCenterList.DataCenters.ContainsKey(maximumVoteAmountCandidate.PublicKey.ToHex()).ShouldBeFalse();
            var subsidy = ProfitItemsIds[ProfitType.BackupSubsidy];
            var profitDetailOfNewCandidate = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(newCandidate.PublicKey)
            });
            profitDetailOfNewCandidate.Details.Count.ShouldBe(1);
            var profitDetailOfMaximumVoteAmountCandidate = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(maximumVoteAmountCandidate.PublicKey)
            });
            profitDetailOfMaximumVoteAmountCandidate.Details.Count.ShouldBe(0);
            
            var minimumVoteAmountCandidate = ValidationDataCenterKeyPairs[0];
            await QuitElectionAsync(minimumVoteAmountCandidate);
            dataCenterList = await ElectionContractStub.GetDataCenterRankingList.CallAsync(new Empty());
            dataCenterList.DataCenters.Count.ShouldBe(fullCount - 1);
            var profitDetailOfMinimumCandidate = await ProfitContractStub.GetProfitDetails.CallAsync(new GetProfitDetailsInput
            {
                SchemeId = subsidy,
                Beneficiary = Address.FromPublicKey(minimumVoteAmountCandidate.PublicKey)
            });
            profitDetailOfMinimumCandidate.Details.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task<List<string>> ElectionContract_GetVictories_ValidCandidatesNotEnough_Test()
        {
            const int amount = 100;

            await NextRound(BootMinerKeyPair);

            foreach (var keyPair in ValidationDataCenterKeyPairs)
            {
                await AnnounceElectionAsync(keyPair);
            }

            var candidates = (await ElectionContractStub.GetCandidates.CallAsync(new Empty())).Value;
            foreach (var fullNodesKeyPair in ValidationDataCenterKeyPairs)
            {
                candidates.ShouldContain(ByteString.CopyFrom(fullNodesKeyPair.PublicKey));
            }

            var validCandidates = ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1).ToList();
            foreach (var keyPair in validCandidates)
            {
                await VoteToCandidate(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, amount);
            }

            foreach (var votedFullNodeKeyPair in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1))
            {
                var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ObtainedActiveVotedVotesAmount.ShouldBe(amount);
            }

            foreach (var votedFullNodeKeyPair in ValidationDataCenterKeyPairs.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1))
            {
                var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
                    {Value = votedFullNodeKeyPair.PublicKey.ToHex()});
                votes.ObtainedActiveVotedVotesAmount.ShouldBe(0);
            }

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            // Victories should contain all valid candidates.
            foreach (var validCandidate in validCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }

            return victories;
        }

        [Fact]
        public async Task<List<ECKeyPair>> ElectionContract_GetVictories_NotAllCandidatesGetVotes_Test()
        {
            await NextRound(BootMinerKeyPair);

            foreach (var keyPair in ValidationDataCenterKeyPairs)
            {
                await AnnounceElectionAsync(keyPair);
            }

            var validCandidates = ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            foreach (var keyPair in validCandidates)
            {
                await VoteToCandidate(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 100);
            }

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            foreach (var validCandidate in validCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }

            return validCandidates;
        }
        
        public async Task<List<string>> ElectionContract_GetVictories_ValidCandidatesEnough_Test()
        {
            await NextRound(BootMinerKeyPair);

            foreach (var keyPair in ValidationDataCenterKeyPairs)
            {
                await AnnounceElectionAsync(keyPair);
            }

            var moreVotesCandidates = ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            foreach (var keyPair in moreVotesCandidates)
            {
                await VoteToCandidate(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 2);
            }

            var lessVotesCandidates = ValidationDataCenterKeyPairs.Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount).Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
            lessVotesCandidates.ForEach(async kp =>
                await VoteToCandidate(VoterKeyPairs[0], kp.PublicKey.ToHex(), 100 * 86400, 1));

            var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
                .Select(p => p.ToHex()).ToList();

            foreach (var validCandidate in moreVotesCandidates)
            {
                victories.ShouldContain(validCandidate.PublicKey.ToHex());
            }

            return victories;
        }

        #endregion

    }
}