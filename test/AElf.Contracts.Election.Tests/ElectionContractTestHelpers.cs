using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Contracts.Election;

public partial class ElectionContractTests : ElectionContractTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ElectionContractTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        InitializeContracts();
    }

    [Fact]
    public async Task ElectionContract_NextTerm_Test()
    {
        await NextTerm(InitialCoreDataCenterKeyPairs[0]);
        var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
        round.TermNumber.ShouldBe(2);
    }

    [Fact]
    public async Task ElectionContract_NormalBlock_Test()
    {
        await NormalBlock(InitialCoreDataCenterKeyPairs[0]);
        var round = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
        round.GetMinedBlocks().ShouldBe(1);
        round.GetMinedMiners().Count.ShouldBe(1);
    }

    private async Task<TransactionResult> AnnounceElectionAsync(ECKeyPair keyPair, Address candidateAdmin = null)
    {
        var electionStub = GetElectionContractTester(keyPair);
        candidateAdmin ??= Address.FromPublicKey(keyPair.PublicKey);
        var announceResult = (await electionStub.AnnounceElection.SendAsync(candidateAdmin))
            .TransactionResult;
        return announceResult;
    }

    private async Task<TransactionResult> QuitElectionAsync(ECKeyPair keyPair)
    {
        var electionStub = GetElectionContractTester(keyPair);
        return (await electionStub.QuitElection.SendAsync(new StringValue { Value = keyPair.PublicKey.ToHex() }))
            .TransactionResult;
    }

    private async Task<TransactionResult> ChangeVotingOption(ECKeyPair voterKeyPair, string candidatePublicKey,
        Hash voteId, bool isResetVotingTime)
    {
        var electionStub = GetElectionContractTester(voterKeyPair);
        var changeVotingResult = (await electionStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
        {
            CandidatePubkey = candidatePublicKey,
            VoteId = voteId,
            IsResetVotingTime = isResetVotingTime
        })).TransactionResult;
        return changeVotingResult;
    }

    private async Task<List<ProfitsClaimed>> ClaimProfitsAsync(ECKeyPair voterKeyPair)
    {
        var profitStub = GetProfitContractTester(voterKeyPair);
        var executionResult = await profitStub.ClaimProfits.SendAsync(new ClaimProfitsInput
        {
            SchemeId = ProfitItemsIds[ProfitType.CitizenWelfare],
            Beneficiary = Address.FromPublicKey(voterKeyPair.PublicKey)
        });
        executionResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        return executionResult.TransactionResult.Logs.Where(l => l.Name == "ProfitsClaimed").Select(l =>
        {
            var logEvent = new ProfitsClaimed();
            logEvent.MergeFrom(l);
            return logEvent;
        }).ToList();
    }

    private async Task<TransactionResult> VoteToCandidateAsync(ECKeyPair voterKeyPair, string candidatePublicKey,
        long lockTime, long amount)
    {
        var electionStub = GetElectionContractTester(voterKeyPair);
        var voteResult = (await electionStub.Vote.SendAsync(new VoteMinerInput
        {
            CandidatePubkey = candidatePublicKey,
            Amount = amount,
            EndTimestamp = TimestampHelper.GetUtcNow().AddSeconds(lockTime)
        })).TransactionResult;

        return voteResult;
    }

    private async Task VoteToCandidateAsync(List<ECKeyPair> votersKeyPairs, string candidatePublicKey,
        int lockTime, long amount)
    {
        foreach (var voterKeyPair in votersKeyPairs)
            await VoteToCandidateAsync(voterKeyPair, candidatePublicKey, lockTime, amount);
    }

    private async Task VoteToCandidates(List<ECKeyPair> votersKeyPairs, List<string> candidatesPublicKeys,
        int lockTime, long amount)
    {
        foreach (var candidatePublicKey in candidatesPublicKeys)
            await VoteToCandidateAsync(votersKeyPairs, candidatePublicKey, lockTime, amount);
    }

    private async Task<TransactionResult> WithdrawVotes(ECKeyPair keyPair, Hash voteId)
    {
        var electionStub = GetElectionContractTester(keyPair);
        return (await electionStub.Withdraw.SendAsync(voteId)).TransactionResult;
    }

    private async Task<TransactionResult> ChangeVoteOption(ECKeyPair keyPair, Hash voteId, string newOption)
    {
        var electionStub = GetElectionContractTester(keyPair);
        return (await electionStub.ChangeVotingOption.SendAsync(new ChangeVotingOptionInput
        {
            VoteId = voteId,
            CandidatePubkey = newOption
        })).TransactionResult;
    }
}