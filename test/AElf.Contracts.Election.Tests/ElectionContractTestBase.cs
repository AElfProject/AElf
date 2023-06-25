using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.VirtualAddress;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.Election;

public class ElectionContractTestBase : EconomicContractsTestBase
{
    protected readonly IBlockTimeProvider BlockTimeProvider;

    protected ElectionContractTestBase()
    {
        BlockTimeProvider = GetRequiredService<IBlockTimeProvider>();
    }
    
    protected Hash MinerElectionVotingItemId;

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub =>
        GetBasicContractTester(BootMinerKeyPair);

    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub =>
        GetTokenContractTester(BootMinerKeyPair);

    internal TokenConverterContractImplContainer.TokenConverterContractImplStub TokenConverterContractStub =>
        GetTokenConverterContractTester(BootMinerKeyPair);

    internal VoteContractImplContainer.VoteContractImplStub VoteContractStub => GetVoteContractTester(BootMinerKeyPair);

    internal ProfitContractImplContainer.ProfitContractImplStub ProfitContractStub =>
        GetProfitContractTester(BootMinerKeyPair);

    internal ElectionContractImplContainer.ElectionContractImplStub ElectionContractStub =>
        GetElectionContractTester(BootMinerKeyPair);

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub =>
        GetAEDPoSContractTester(BootMinerKeyPair);

    internal TreasuryContractImplContainer.TreasuryContractImplStub TreasuryContractStub =>
        GetTreasuryContractTester(BootMinerKeyPair);

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
        GetParliamentContractTester(BootMinerKeyPair);

    internal EconomicContractImplContainer.EconomicContractImplStub EconomicContractStub =>
        GetEconomicContractTester(BootMinerKeyPair);

    internal VirtualAddressContractContainer.VirtualAddressContractStub VirtualAddressContractStub =>
        GetVirtualAddressContractTester(BootMinerKeyPair);

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
        AsyncHelper.RunSync(InitialMiningRewards);

        MinerElectionVotingItemId = AsyncHelper.RunSync(() =>
            ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty()));
    }

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetBasicContractTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
    }

    internal TokenContractImplContainer.TokenContractImplStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
    }

    internal TokenConverterContractImplContainer.TokenConverterContractImplStub GetTokenConverterContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<TokenConverterContractImplContainer.TokenConverterContractImplStub>(
            TokenConverterContractAddress,
            keyPair);
    }

    internal VoteContractImplContainer.VoteContractImplStub GetVoteContractTester(ECKeyPair keyPair)
    {
        return GetTester<VoteContractImplContainer.VoteContractImplStub>(VoteContractAddress, keyPair);
    }

    internal ProfitContractImplContainer.ProfitContractImplStub GetProfitContractTester(ECKeyPair keyPair)
    {
        return GetTester<ProfitContractImplContainer.ProfitContractImplStub>(ProfitContractAddress, keyPair);
    }

    internal ElectionContractImplContainer.ElectionContractImplStub GetElectionContractTester(ECKeyPair keyPair)
    {
        return GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress, keyPair);
    }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetAEDPoSContractTester(ECKeyPair keyPair)
    {
        return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
    }

    internal TreasuryContractImplContainer.TreasuryContractImplStub GetTreasuryContractTester(ECKeyPair keyPair)
    {
        return GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress, keyPair);
    }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    internal EconomicContractImplContainer.EconomicContractImplStub GetEconomicContractTester(ECKeyPair keyPair)
    {
        return GetTester<EconomicContractImplContainer.EconomicContractImplStub>(EconomicContractAddress, keyPair);
    }
    
    internal VirtualAddressContractContainer.VirtualAddressContractStub GetVirtualAddressContractTester(ECKeyPair keyPair)
    {
        return GetTester<VirtualAddressContractContainer.VirtualAddressContractStub>(VirtualAddressContractAddress, keyPair);
    }
}