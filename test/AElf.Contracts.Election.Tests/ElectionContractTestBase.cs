using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.Election
{
    public class ElectionContractTestBase : EconomicContractsTestBase
    {
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

        protected Hash MinerElectionVotingItemId;

        internal new BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub =>
            GetBasicContractTester(BootMinerKeyPair);

        internal new TokenContractImplContainer.TokenContractImplStub TokenContractStub => GetTokenContractTester(BootMinerKeyPair);

        internal new TokenConverterContractImplContainer.TokenConverterContractImplStub TokenConverterContractStub =>
            GetTokenConverterContractTester(BootMinerKeyPair);

        internal new VoteContractImplContainer.VoteContractImplStub VoteContractStub => GetVoteContractTester(BootMinerKeyPair);

        internal new ProfitContractImplContainer.ProfitContractImplStub ProfitContractStub =>
            GetProfitContractTester(BootMinerKeyPair);

        internal new ElectionContractImplContainer.ElectionContractImplStub ElectionContractStub =>
            GetElectionContractTester(BootMinerKeyPair);

        internal new AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub =>
            GetAEDPoSContractTester(BootMinerKeyPair);

        internal new TreasuryContractImplContainer.TreasuryContractImplStub TreasuryContractStub =>
            GetTreasuryContractTester(BootMinerKeyPair);

        internal new ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
            GetParliamentContractTester(BootMinerKeyPair);

        internal new EconomicContractImplContainer.EconomicContractImplStub EconomicContractStub =>
            GetEconomicContractTester(BootMinerKeyPair);

        internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetBasicContractTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
        }

        internal new TokenContractImplContainer.TokenContractImplStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
        }

        internal new TokenConverterContractImplContainer.TokenConverterContractImplStub GetTokenConverterContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<TokenConverterContractImplContainer.TokenConverterContractImplStub>(TokenConverterContractAddress,
                keyPair);
        }

        internal new VoteContractImplContainer.VoteContractImplStub GetVoteContractTester(ECKeyPair keyPair)
        {
            return GetTester<VoteContractImplContainer.VoteContractImplStub>(VoteContractAddress, keyPair);
        }

        internal new ProfitContractImplContainer.ProfitContractImplStub GetProfitContractTester(ECKeyPair keyPair)
        {
            return GetTester<ProfitContractImplContainer.ProfitContractImplStub>(ProfitContractAddress, keyPair);
        }

        internal new ElectionContractImplContainer.ElectionContractImplStub GetElectionContractTester(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractImplContainer.ElectionContractImplStub>(ElectionContractAddress, keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetAEDPoSContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }

        internal new TreasuryContractImplContainer.TreasuryContractImplStub GetTreasuryContractTester(ECKeyPair keyPair)
        {
            return GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(TreasuryContractAddress, keyPair);
        }

        internal new ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                keyPair);
        }

        internal new EconomicContractImplContainer.EconomicContractImplStub GetEconomicContractTester(ECKeyPair keyPair)
        {
            return GetTester<EconomicContractImplContainer.EconomicContractImplStub>(EconomicContractAddress, keyPair);
        }
    }
}