using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election;

public partial class ElectionContractTests : ElectionContractTestBase
{
    [Fact]
    public async Task GetMinersCount_Test()
    {
        var minersCount = await ElectionContractStub.GetMinersCount.CallAsync(new Empty());
        minersCount.Value.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount);
    }

    [Fact]
    public async Task GetElectionResult_Test()
    {
        await ElectionContract_Vote_Test();
        await NextTerm(InitialCoreDataCenterKeyPairs[0]);

        //verify term 1
        var electionResult = await ElectionContractStub.GetElectionResult.CallAsync(new GetElectionResultInput
        {
            TermNumber = 1
        });
        electionResult.IsActive.ShouldBe(false);
        electionResult.Results.Count.ShouldBe(19);
        electionResult.Results.Values.ShouldAllBe(o => o == 1000);
    }

    [Fact]
    public async Task GetElectorVoteWithRecords_NotExist_Test()
    {
        await ElectionContract_Vote_Test();

        var voteRecords = await ElectionContractStub.GetElectorVoteWithRecords.CallAsync(new StringValue
        {
            Value = ValidationDataCenterKeyPairs.Last().PublicKey.ToHex()
        });
        voteRecords.ShouldBe(new ElectorVote());
    }

    [Fact]
    public async Task GetElectorVoteWithAllRecords_Test()
    {
        var voters = await UserVotesCandidate(2, 500, 100);
        var voterKeyPair = voters[0];
        //without withdraw
        var allRecords = await ElectionContractStub.GetElectorVoteWithAllRecords.CallAsync(new StringValue
        {
            Value = voterKeyPair.PublicKey.ToHex()
        });
        allRecords.ActiveVotingRecords.Count.ShouldBeGreaterThanOrEqualTo(1);
        allRecords.WithdrawnVotingRecordIds.Count.ShouldBe(0);

        //withdraw
        await NextTerm(InitialCoreDataCenterKeyPairs[0]);
        BlockTimeProvider.SetBlockTime(StartTimestamp.AddSeconds(100 * 60 * 60 * 24 + 1));
        var voteId =
            (await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
                { Value = voterKeyPair.PublicKey.ToHex() })).ActiveVotingRecordIds.First();
        var executionResult = await WithdrawVotes(voterKeyPair, voteId);
        executionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        allRecords = await ElectionContractStub.GetElectorVoteWithAllRecords.CallAsync(new StringValue
        {
            Value = voterKeyPair.PublicKey.ToHex()
        });
        allRecords.WithdrawnVotingRecordIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetVotersCount_Test()
    {
        await UserVotesCandidate(5, 1000, 120);

        var votersCount = await ElectionContractStub.GetVotersCount.CallAsync(new Empty());
        votersCount.Value.ShouldBe(5 * CandidatesCount);
    }

    [Fact]
    public async Task GetVotesAmount_Test()
    {
        await UserVotesCandidate(2, 200, 120);

        var votesAmount = await ElectionContractStub.GetVotesAmount.CallAsync(new Empty());
        votesAmount.Value.ShouldBe(2 * CandidatesCount * 200);
    }

    [Fact]
    public async Task GetTermSnapshot_Test()
    {
        //first term
        {
            await ProduceBlocks(InitialCoreDataCenterKeyPairs[0], 5);
            await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
            await ProduceBlocks(InitialCoreDataCenterKeyPairs[2], 15);
            await NextTerm(BootMinerKeyPair);

            var snapshot = await ElectionContractStub.GetTermSnapshot.CallAsync(new GetTermSnapshotInput
            {
                TermNumber = 1
            });
            snapshot.MinedBlocks.ShouldBeGreaterThanOrEqualTo(30);
            snapshot.ElectionResult.Count.ShouldBe(0);
        }

        //second term
        {
            foreach (var keyPair in ValidationDataCenterKeyPairs) await AnnounceElectionAsync(keyPair);

            var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
            candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);

            var moreVotesCandidates = ValidationDataCenterKeyPairs
                .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();

            foreach (var candidate in moreVotesCandidates)
                await VoteToCandidateAsync(VoterKeyPairs[0], candidate.PublicKey.ToHex(), 100 * 86400, 2);

            await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
            await NextTerm(BootMinerKeyPair);

            var snapshot = await ElectionContractStub.GetTermSnapshot.CallAsync(new GetTermSnapshotInput
            {
                TermNumber = 2
            });
            snapshot.MinedBlocks.ShouldBeGreaterThanOrEqualTo(10);
            snapshot.ElectionResult.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);
            snapshot.ElectionResult.Values
                .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToArray()
                .ShouldAllBe(item => item == 2);
        }
    }

    [Fact]
    public async Task GetPageableCandidateInformation_Test()
    {
        foreach (var keyPair in ValidationDataCenterKeyPairs) await AnnounceElectionAsync(keyPair);

        //query before vote
        var candidateInformation0 =
            await ElectionContractStub.GetPageableCandidateInformation.CallAsync(new PageInformation
            {
                Start = 0,
                Length = 10
            });
        candidateInformation0.Value.Count.ShouldBe(10);
        candidateInformation0.Value.ToList().Select(o => o.ObtainedVotesAmount).ShouldAllBe(o => o == 0);

        var candidates = await ElectionContractStub.GetCandidates.CallAsync(new Empty());
        candidates.Value.Count.ShouldBe(ValidationDataCenterKeyPairs.Count);
        var moreVotesCandidates = ValidationDataCenterKeyPairs
            .Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
        foreach (var keyPair in moreVotesCandidates)
            await VoteToCandidateAsync(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 2);
        var fewVotesCandidates = ValidationDataCenterKeyPairs
            .Skip(EconomicContractsTestConstants.InitialCoreDataCenterCount).Take(10).ToList();
        foreach (var keyPair in fewVotesCandidates)
            await VoteToCandidateAsync(VoterKeyPairs[0], keyPair.PublicKey.ToHex(), 100 * 86400, 1);

        var candidateInformation =
            await ElectionContractStub.GetPageableCandidateInformation.CallAsync(new PageInformation
            {
                Start = 0,
                Length = 5
            });
        candidateInformation.Value.Count.ShouldBe(5);
        candidateInformation.Value.ToList().Select(o => o.ObtainedVotesAmount).ShouldAllBe(o => o == 2);

        var candidateInformation1 =
            await ElectionContractStub.GetPageableCandidateInformation.CallAsync(new PageInformation
            {
                Start = 5,
                Length = 10
            });
        candidateInformation1.Value.Count.ShouldBe(10);
        candidateInformation1.Value.ToList().Select(o => o.ObtainedVotesAmount).ShouldAllBe(o => o == 1);
    }

    [Fact]
    public async Task GetCurrentMiningReward_Test()
    {
        await NextTerm(BootMinerKeyPair);

        //basic value
        {
            await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
            var miningReward = await AEDPoSContractStub.GetCurrentTermMiningReward.CallAsync(new Empty());
            miningReward.Value.ShouldBeGreaterThanOrEqualTo(ElectionContractConstants.ElfTokenPerBlock * 10);
        }

        //compare with different term
        {
            await NextTerm(BootMinerKeyPair);
            await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
            var miningReward1 = await AEDPoSContractStub.GetCurrentTermMiningReward.CallAsync(new Empty());

            await NextTerm(BootMinerKeyPair);
            await ProduceBlocks(InitialCoreDataCenterKeyPairs[1], 10);
            var miningReward2 = await AEDPoSContractStub.GetCurrentTermMiningReward.CallAsync(new Empty());

            miningReward1.ShouldBe(miningReward2);
        }
    }

    [Fact]
    public async Task Vote_Weight_Calculate()
    {
        var day = 375; // in 2 year
        var weight = await ElectionContractStub.GetCalculateVoteWeight.CallAsync(new VoteInformation
        {
            Amount = 1000,
            LockTime = day * 24 * 3600
        });
        weight.Value.ShouldBe(2254);
        day = 180; // in 1 year
        weight = await ElectionContractStub.GetCalculateVoteWeight.CallAsync(new VoteInformation
        {
            Amount = 1000,
            LockTime = day * 24 * 3600
        });
        weight.Value.ShouldBe(1697);
        day = 1000; // in 3 year
        weight = await ElectionContractStub.GetCalculateVoteWeight.CallAsync(new VoteInformation
        {
            Amount = 1000,
            LockTime = day * 24 * 3600
        });
        weight.Value.ShouldBe(7874);
        day = 1096; // > 3 year
        weight = await ElectionContractStub.GetCalculateVoteWeight.CallAsync(new VoteInformation
        {
            Amount = 1000,
            LockTime = day * 24 * 3600
        });
        weight.Value.ShouldBe(9433);

        day = 1; // 1 day
        weight = await ElectionContractStub.GetCalculateVoteWeight.CallAsync(new VoteInformation
        {
            Amount = 1000,
            LockTime = day * 24 * 3600
        });
        weight.Value.ShouldBe(1500);
    }

    [Fact]
    public async Task Election_GetMinerElectionVotingItemId_Test()
    {
        var voteItemId = await ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty());
        var voteItem = await VoteContractStub.GetVotingItem.CallAsync(new GetVotingItemInput
        {
            VotingItemId = voteItemId
        });
        voteItem.IsLockToken.ShouldBe(false);
    }

    [Fact]
    public async Task Election_GetVotedCandidates_Test()
    {
        var ret = await ElectionContractStub.GetVotedCandidates.CallAsync(new Empty());
        ret.ShouldBe(new PubkeyList());
    }

    [Fact]
    public async Task Election_GetCandidateInformation_Test()
    {
        var key = "not exist";
        var ret = await ElectionContractStub.GetCandidateInformation.CallAsync(new StringValue
        {
            Value = key
        });
        ret.ShouldBe(new CandidateInformation { Pubkey = key });
    }

    [Fact]
    public async Task Election_GetTermSnapshot_Test()
    {
        var ret = await ElectionContractStub.GetTermSnapshot.CallAsync(new GetTermSnapshotInput
        {
            TermNumber = 10
        });
        ret.ShouldBe(new TermSnapshot());
    }

    [Fact]
    public async Task Election_GetElectorVote_Test()
    {
        var key = ValidationDataCenterKeyPairs.First().PublicKey.ToHex();
        var ret = await ElectionContractStub.GetElectorVote.CallAsync(new StringValue
        {
            Value = key
        });
        ret.ShouldBe(new ElectorVote());
    }

    [Fact]
    public async Task Election_GetPageableCandidateInformation_Test()
    {
        var ret = await ElectionContractStub.GetPageableCandidateInformation.CallAsync(new PageInformation
        {
            Start = 100
        });
        ret.ShouldBe(new GetPageableCandidateInformationOutput());
    }

    [Fact]
    public async Task Election_GetVoteWeightInterestController_Test()
    {
        var ret = await ElectionContractStub.GetVoteWeightInterestController.CallAsync(new Empty());
        var parliamentDefaultAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        ret.OwnerAddress.ShouldBe(parliamentDefaultAddress);
        ret.ContractAddress.ShouldBe(ParliamentContractAddress);
    }

    private async Task<List<ECKeyPair>> UserVotesCandidate(int voterCount, long voteAmount, int lockDays)
    {
        var lockTime = lockDays * 60 * 60 * 24;

        var candidatesKeyPairs = await ElectionContract_AnnounceElection_Test();

        var votersKeyPairs = VoterKeyPairs.Take(voterCount).ToList();
        var voterKeyPair = votersKeyPairs[0];
        var balanceBeforeVoting = await GetNativeTokenBalance(voterKeyPair.PublicKey);
        balanceBeforeVoting.ShouldBeGreaterThan(0);

        await VoteToCandidates(votersKeyPairs,
            candidatesKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(), lockTime, voteAmount);

        return votersKeyPairs;
    }
}