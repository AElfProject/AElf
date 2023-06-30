using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.Consensus.AEDPoS;

public class AEDPoSContractTestBase : EconomicContractsTestBase
{
    protected IAElfAsymmetricCipherKeyPairProvider KeyPairProvider =>
        Application.ServiceProvider.GetRequiredService<IAElfAsymmetricCipherKeyPairProvider>();

    protected ITriggerInformationProvider TriggerInformationProvider =>
        Application.ServiceProvider.GetRequiredService<ITriggerInformationProvider>();

    protected Timestamp BlockchainStartTimestamp => TimestampHelper.GetUtcNow();

    protected IBlockchainService BlockchainService =>
        Application.ServiceProvider.GetRequiredService<IBlockchainService>();

    internal TokenContractContainer.TokenContractStub TokenContractStub => GetTokenContractTester(BootMinerKeyPair);

    internal VoteContractContainer.VoteContractStub VoteContractStub => GetVoteContractTester(BootMinerKeyPair);

    internal ProfitContractContainer.ProfitContractStub ProfitContractStub =>
        GetProfitContractTester(BootMinerKeyPair);

    internal ElectionContractContainer.ElectionContractStub ElectionContractStub =>
        GetElectionContractTester(BootMinerKeyPair);

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub =>
        GetAEDPoSContractStub(BootMinerKeyPair);

    internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub =>
        GetTreasuryContractTester(BootMinerKeyPair);

    internal EconomicContractContainer.EconomicContractStub EconomicContractStub =>
        GetEconomicContractTester(BootMinerKeyPair);

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
        GetParliamentContractTester(BootMinerKeyPair);

    private new void DeployAllContracts()
    {
        _ = TokenContractAddress;
        _ = VoteContractAddress;
        _ = ProfitContractAddress;
        _ = EconomicContractAddress;
        _ = ElectionContractAddress;
        _ = TreasuryContractAddress;
        _ = TransactionFeeChargingContractAddress;
        _ = ParliamentContractAddress;
        _ = TokenConverterContractAddress;
        _ = ConsensusContractAddress;
        _ = ReferendumContractAddress;
        _ = TokenHolderContractAddress;
        _ = AssociationContractAddress;
    }

    protected void InitializeContracts()
    {
        DeployAllContracts();

        AsyncHelper.RunSync(InitializeParliamentContract);
        AsyncHelper.RunSync(InitializeTreasuryConverter);
        AsyncHelper.RunSync(InitializeElection);
        AsyncHelper.RunSync(InitializeEconomicContract);
        AsyncHelper.RunSync(InitializeToken);
        AsyncHelper.RunSync(InitializeAElfConsensus);
    }

    internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
    }

    internal VoteContractContainer.VoteContractStub GetVoteContractTester(ECKeyPair keyPair)
    {
        return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, keyPair);
    }

    internal ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
    {
        return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
    }

    internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
    {
        return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
    }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetAEDPoSContractStub(ECKeyPair keyPair)
    {
        return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
    }

    internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractTester(ECKeyPair keyPair)
    {
        return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
    }

    internal EconomicContractContainer.EconomicContractStub GetEconomicContractTester(ECKeyPair keyPair)
    {
        return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
    }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    protected async Task InitializeCandidates(int take = EconomicContractsTestConstants.ValidateDataCenterCount)
    {
        foreach (var candidatesKeyPair in ValidationDataCenterKeyPairs.Take(take))
        {
            var electionTester = GetElectionContractTester(candidatesKeyPair);
            var announceResult =
                await electionTester.AnnounceElection.SendAsync(Address.FromPublicKey(candidatesKeyPair.PublicKey));
            announceResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //query candidates
            var candidates = await electionTester.GetCandidates.CallAsync(new Empty());
            candidates.Value.Select(o => o.ToByteArray().ToHex()).Contains(candidatesKeyPair.PublicKey.ToHex())
                .ShouldBeTrue();
        }
    }

    protected async Task BootMinerChangeRoundAsync(long nextRoundNumber = 2)
    {
        var currentRound = await AEDPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
        var expectedStartTime = BlockchainStartTimestamp.ToDateTime()
            .AddMilliseconds(
                ((long)currentRound.TotalMilliseconds(AEDPoSContractTestConstants.MiningInterval)).Mul(
                    nextRoundNumber.Sub(1)));
        var randomNumber = await GenerateRandomProofAsync(BootMinerKeyPair);
        currentRound.GenerateNextRoundInformation(expectedStartTime.ToTimestamp(), BlockchainStartTimestamp,
            ByteString.CopyFrom(randomNumber), out var nextRound);
        await AEDPoSContractStub.NextRound.SendAsync(nextRound);
    }

    protected async Task<Hash> CreateProposalAsync(Address contractAddress, Address organizationAddress,
        string methodName, IMessage input)
    {
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = organizationAddress,
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
            Params = input.ToByteString(),
            ToAddress = contractAddress
        };

        var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
        var proposalId = createResult.Output;

        return proposalId;
    }

    protected async Task ApproveWithMinersAsync(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(proposalId);
            approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        }
    }
}