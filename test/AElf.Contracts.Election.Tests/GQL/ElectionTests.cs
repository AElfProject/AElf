using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election;

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
            WelcomeHash = HashHelper.ComputeFrom("Welcome"),
            FlexibleHash = HashHelper.ComputeFrom("Flexible")
        });
        setSchemeIdRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        setSchemeIdRet.TransactionResult.Error.ShouldContain("Treasury profit ids already set.");
    }

    #region Vote

    [Fact]
    public async Task ElectionContract_Vote_Failed_Test()
    {
        var candidateKeyPair = ValidationDataCenterKeyPairs[0];
        var voterKeyPair = VoterKeyPairs[0];

        // candidateKeyPair not announced election yet.
        {
            var transactionResult =
                await VoteToCandidateAsync(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400, 100);

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
                await VoteToCandidateAsync(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 120 * 86400, voterBalance + 10);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("Insufficient balance");
        }

        // Lock time is less than 7 days
        {
            var transactionResult =
                await VoteToCandidateAsync(voterKeyPair, candidateKeyPair.PublicKey.ToHex(), 5 * 86400, 1000);

            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.ShouldContain("lock time");
        }
    }

    #endregion

    #region Withdraw

    [Fact]
    public async Task Election_Withdraw_In_LockTime_Test()
    {
        var voter = VoterKeyPairs.First();
        var voteAmount = 100;
        var lockTime = 120 * 60 * 60 * 24;
        var candidate = ValidationDataCenterKeyPairs.First();
        await AnnounceElectionAsync(candidate);
        await VoteToCandidateAsync(voter, candidate.PublicKey.ToHex(), lockTime, voteAmount);
        var electionVoteItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
        var voteIdOfVoter = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
        {
            Voter = Address.FromPublicKey(voter.PublicKey),
            VotingItemId = electionVoteItemId
        });
        var voteId = voteIdOfVoter.ActiveVotes[0];
        var withdrawRet = await WithdrawVotes(voter, voteId);
        withdrawRet.Status.ShouldBe(TransactionResultStatus.Failed);
        withdrawRet.Error.ShouldContain("days to unlock your token");
    }

    #endregion

    [Fact]
    public async Task Election_TakeSnapshot_Without_Authority_Test()
    {
        var takeSnapshot = await ElectionContractStub.TakeSnapshot.SendAsync(new TakeElectionSnapshotInput
        {
            TermNumber = 1
        });
        takeSnapshot.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        takeSnapshot.TransactionResult.Error.ShouldContain("No permission");
    }

    [Fact]
    public async Task Election_UpdateCandidateInformation_Without_Authority_Test()
    {
        var pubkey = ValidationDataCenterKeyPairs.First().PublicKey.ToHex();
        var transactionResult = (await ElectionContractStub.UpdateCandidateInformation.SendAsync(
            new UpdateCandidateInformationInput
            {
                IsEvilNode = true,
                Pubkey = pubkey,
                RecentlyProducedBlocks = 10,
                RecentlyMissedTimeSlots = 100
            })).TransactionResult;

        transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.Error.ShouldContain("Only consensus contract can update candidate information");
    }

    [Fact]
    public async Task Election_UpdateMultipleCandidateInformation_Without_Authority_Test()
    {
        var pubkey = ValidationDataCenterKeyPairs.First().PublicKey.ToHex();
        var updateInfo = new UpdateMultipleCandidateInformationInput
        {
            Value =
            {
                new UpdateCandidateInformationInput
                {
                    IsEvilNode = true,
                    Pubkey = pubkey,
                    RecentlyProducedBlocks = 10,
                    RecentlyMissedTimeSlots = 100
                }
            }
        };

        var transactionResult = (await ElectionContractStub.UpdateMultipleCandidateInformation.SendAsync(updateInfo)
            ).TransactionResult;

        transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.Error.ShouldContain("Only consensus contract can update candidate information");
    }

    [Fact]
    public async Task Election_UpdateMinersCount_Without_Authority_Test()
    {
        var transactionResult = (await ElectionContractStub.UpdateMinersCount.SendAsync(new UpdateMinersCountInput
        {
            MinersCount = 10
        })).TransactionResult;

        transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.Error.ShouldContain("Only consensus contract can update miners count");
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
        transactionResult.Error.Contains("Target is not a candidate").ShouldBeTrue();
    }

    [Fact]
    public async Task ElectionContract_QuitElection_MinerQuit_Test()
    {
        await NextRound(BootMinerKeyPair);
        var voter = VoterKeyPairs.First();
        var voteAmount = 100;
        var lockTime = 120 * 60 * 60 * 24;
        var candidate = ValidationDataCenterKeyPairs.First();
        await AnnounceElectionAsync(candidate);
        await VoteToCandidateAsync(voter, candidate.PublicKey.ToHex(), lockTime, voteAmount);
        var victories = await ElectionContractStub.GetVictories.CallAsync(new Empty());
        victories.Value.Contains(ByteStringHelper.FromHexString(candidate.PublicKey.ToHex())).ShouldBeTrue();
        await NextTerm(InitialCoreDataCenterKeyPairs[0]);
        var quitElectionRet = await QuitElectionAsync(candidate);
        quitElectionRet.Status.ShouldBe(TransactionResultStatus.Failed);
        quitElectionRet.Error.ShouldContain("Current miners cannot quit election");
    }

    #endregion

    #region ChangeVotingOption

    [Fact]
    public async Task Election_ChangeVotingOption_Not_Voter_Test()
    {
        var voter = VoterKeyPairs.First();
        var voteAmount = 100;
        var lockTime = 120 * 60 * 60 * 24;
        var candidate = ValidationDataCenterKeyPairs.First();
        await AnnounceElectionAsync(candidate);
        await VoteToCandidateAsync(voter, candidate.PublicKey.ToHex(), lockTime, voteAmount);
        var electionVoteItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
        var voteIdOfVoter = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
        {
            Voter = Address.FromPublicKey(voter.PublicKey),
            VotingItemId = electionVoteItemId
        });
        var voteId = voteIdOfVoter.ActiveVotes[0];
        var changeOptionRet = await ElectionContractStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
        {
            CandidatePubkey = candidate.PublicKey.ToHex(),
            VoteId = voteId
        });
        changeOptionRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        changeOptionRet.TransactionResult.Error.ShouldContain("No permission to change current vote's option.");
    }

    [Fact]
    public async Task Election_ChangeVotingOption_With_Expire_Vote_Test()
    {
        var voter = VoterKeyPairs.First();
        var voteAmount = 100;
        var lockTime = 120 * 60 * 60 * 24;
        var candidate = ValidationDataCenterKeyPairs.First();
        await AnnounceElectionAsync(candidate);
        await VoteToCandidateAsync(voter, candidate.PublicKey.ToHex(), lockTime, voteAmount);
        var electionVoteItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
        var voteIdOfVoter = await VoteContractStub.GetVotingIds.CallAsync(new GetVotingIdsInput
        {
            Voter = Address.FromPublicKey(voter.PublicKey),
            VotingItemId = electionVoteItemId
        });
        var voteId = voteIdOfVoter.ActiveVotes[0];
        BlockTimeProvider.SetBlockTime(StartTimestamp.AddSeconds(lockTime + 1));
        var changeOptionRet = await ChangeVoteOption(voter, voteId, candidate.PublicKey.ToHex());
        changeOptionRet.Status.ShouldBe(TransactionResultStatus.Failed);
        changeOptionRet.Error.ShouldContain("This vote already expired");
    }

    [Fact]
    public async Task ElectionContract_ChangeVoting_To_Invalid_Target()
    {
        var invalidCandidateKeyPair = "invalid key";
        var ret = await ElectionContractStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
        {
            CandidatePubkey = invalidCandidateKeyPair,
            VoteId = new Hash()
        });
        var errorMsg = "Candidate not found.";
        ret.TransactionResult.Error.ShouldContain(errorMsg);
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
            victories.ShouldContain(initialMiner);
    }

    [Fact]
    public async Task ElectionContract_GetVictories_CandidatesNotEnough_Test()
    {
        // To get previous round information.
        await NextRound(BootMinerKeyPair);

        var keyPairs = ValidationDataCenterKeyPairs
            .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1).ToList();
        foreach (var keyPair in keyPairs) await AnnounceElectionAsync(keyPair);

        var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
            .Select(p => p.ToHex()).ToList();

        // Same as initial miners.
        victories.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
        foreach (var initialMiner in InitialCoreDataCenterKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            victories.ShouldContain(initialMiner);
    }

    [Fact]
    public async Task ElectionContract_GetVictories_NoValidCandidate_Test()
    {
        await NextRound(BootMinerKeyPair);

        foreach (var keyPair in ValidationDataCenterKeyPairs) await AnnounceElectionAsync(keyPair);

        var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
            .Select(p => p.ToHex()).ToList();

        // Same as initial miners.
        victories.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
        foreach (var initialMiner in InitialCoreDataCenterKeyPairs.Select(kp => kp.PublicKey.ToHex()))
            victories.ShouldContain(initialMiner);
    }

    [Fact]
    public async Task ElectionContract_ToBecomeValidationDataCenter_Test()
    {
        foreach (var keyPair in ValidationDataCenterKeyPairs.Take(25)) await AnnounceElectionAsync(keyPair);

        //add new candidate and vote into data center
        var newCandidate = ValidationDataCenterCandidateKeyPairs.First();
        await AnnounceElectionAsync(newCandidate);

        var voter = VoterKeyPairs.First();
        await
            VoteToCandidateAsync(voter, newCandidate.PublicKey.ToHex(), 100 * 86400, 200);

        var victories = await ElectionContractStub.GetVictories.CallAsync(new Empty());
        victories.Value.Select(o => o.ToHex()).ShouldContain(newCandidate.PublicKey.ToHex());
    }

    [Fact]
    public async Task<List<string>> ElectionContract_GetVictories_ValidCandidatesNotEnough_Test()
    {
        const int amount = 100;

        await NextRound(BootMinerKeyPair);

        foreach (var keyPair in ValidationDataCenterKeyPairs) await AnnounceElectionAsync(keyPair);

        var candidates = (await ElectionContractStub.GetCandidates.CallAsync(new Empty())).Value;
        foreach (var fullNodesKeyPair in ValidationDataCenterKeyPairs)
            candidates.ShouldContain(ByteString.CopyFrom(fullNodesKeyPair.PublicKey));

        var validCandidates = ValidationDataCenterKeyPairs
            .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1).ToList();
        foreach (var keyPair in validCandidates)
            await VoteToCandidateAsync(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, amount);

        foreach (var votedFullNodeKeyPair in ValidationDataCenterKeyPairs.Take(EconomicContractsTestConstants
                     .InitialCoreDataCenterCount - 1))
        {
            var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
                { Value = votedFullNodeKeyPair.PublicKey.ToHex() });
            votes.ObtainedActiveVotedVotesAmount.ShouldBe(amount);
        }

        foreach (var votedFullNodeKeyPair in ValidationDataCenterKeyPairs.Skip(EconomicContractsTestConstants
                     .InitialCoreDataCenterCount - 1))
        {
            var votes = await ElectionContractStub.GetCandidateVote.CallAsync(new StringValue
                { Value = votedFullNodeKeyPair.PublicKey.ToHex() });
            votes.ObtainedActiveVotedVotesAmount.ShouldBe(0);
        }

        var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
            .Select(p => p.ToHex()).ToList();

        // Victories should contain all valid candidates.
        foreach (var validCandidate in validCandidates) victories.ShouldContain(validCandidate.PublicKey.ToHex());

        return victories;
    }

    [Fact]
    public async Task<List<ECKeyPair>> ElectionContract_GetVictories_NotAllCandidatesGetVotes_Test()
    {
        await NextRound(BootMinerKeyPair);

        foreach (var keyPair in ValidationDataCenterKeyPairs) await AnnounceElectionAsync(keyPair);

        var validCandidates = ValidationDataCenterKeyPairs
            .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
        foreach (var keyPair in validCandidates)
            await VoteToCandidateAsync(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 100);

        var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
            .Select(p => p.ToHex()).ToList();

        foreach (var validCandidate in validCandidates) victories.ShouldContain(validCandidate.PublicKey.ToHex());

        return validCandidates;
    }

    public async Task<List<string>> ElectionContract_GetVictories_ValidCandidatesEnough_Test()
    {
        await NextRound(BootMinerKeyPair);

        foreach (var keyPair in ValidationDataCenterKeyPairs) await AnnounceElectionAsync(keyPair);

        var moreVotesCandidates = ValidationDataCenterKeyPairs
            .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
        foreach (var keyPair in moreVotesCandidates)
            await VoteToCandidateAsync(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 2);

        var lessVotesCandidates = ValidationDataCenterKeyPairs
            .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount)
            .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
        foreach (var candidate in lessVotesCandidates)
            await VoteToCandidateAsync(VoterKeyPairs[0], candidate.PublicKey.ToHex(), 100 * 86400, 1);

        var victories = (await ElectionContractStub.GetVictories.CallAsync(new Empty())).Value
            .Select(p => p.ToHex()).ToList();

        foreach (var validCandidate in moreVotesCandidates) victories.ShouldContain(validCandidate.PublicKey.ToHex());

        return victories;
    }

    #endregion
}