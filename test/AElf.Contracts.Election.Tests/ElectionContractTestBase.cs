using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
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
            var tokenContract = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(TokenContractAddress, Hash.FromRawBytes(tokenContract)));
            
            _ = VoteContractAddress;
            var voteContractCode = Codes.Single(kv => kv.Key.Contains("Vote")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(VoteContractAddress, Hash.FromRawBytes(voteContractCode)));
            
            _ = ProfitContractAddress;
            var profitContractCode = Codes.Single(kv => kv.Key.Contains("Profit")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(ProfitContractAddress, Hash.FromRawBytes(profitContractCode)));
            
            _ = EconomicContractAddress;
            var economicContractCode = Codes.Single(kv => kv.Key.Contains("Economic")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(EconomicContractAddress, Hash.FromRawBytes(economicContractCode)));
            
            _ = ElectionContractAddress;
            var electionContractCode = Codes.Single(kv => kv.Key.Contains("Election")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(ProfitContractAddress, Hash.FromRawBytes(electionContractCode)));
            
            _ = TreasuryContractAddress;
            var treasuryContractCode = Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(TreasuryContractAddress, Hash.FromRawBytes(treasuryContractCode)));
            
            _ = TransactionFeeChargingContractAddress;
            var transactionFeeChargingContractCode = Codes.Single(kv => kv.Key.Contains("TransactionFee")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(TransactionFeeChargingContractAddress, Hash.FromRawBytes(transactionFeeChargingContractCode)));
            
            _ = ParliamentAuthContractAddress;
            var parliamentAuthContractCode = Codes.Single(kv => kv.Key.Contains("ParliamentAuth")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(ParliamentAuthContractAddress, Hash.FromRawBytes(parliamentAuthContractCode)));
            
            _ = TokenConverterContractAddress;
            var tokenConverterContractCode = Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(TokenConverterContractAddress, Hash.FromRawBytes(tokenConverterContractCode)));
            
            _ = ConsensusContractAddress;
            var consensusContractCode = Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(ConsensusContractAddress, Hash.FromRawBytes(consensusContractCode)));
            
            _ = ReferendumAuthContractAddress;
            var referendumAuthContractCode = Codes.Single(kv => kv.Key.Contains("ReferendumAuth")).Value;
            AsyncHelper.RunSync(()=>SetContractCacheAsync(ReferendumAuthContractAddress, Hash.FromRawBytes(referendumAuthContractCode)));
            
            
        }

        protected void InitializeContracts()
        {
            DeployAllContracts();

            AsyncHelper.RunSync(InitializeTreasuryConverter);
            AsyncHelper.RunSync(InitializeElection);
            AsyncHelper.RunSync(InitializeParliamentContract);
            AsyncHelper.RunSync(InitializeEconomicContract);
            AsyncHelper.RunSync(InitializeToken);
            AsyncHelper.RunSync(InitializeAElfConsensus);
            AsyncHelper.RunSync(InitialMiningRewards);

            MinerElectionVotingItemId = AsyncHelper.RunSync(() =>
                ElectionContractStub.GetMinerElectionVotingItemId.CallAsync(new Empty()));
        }

        protected Hash MinerElectionVotingItemId;

        internal new BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub =>
            GetBasicContractTester(BootMinerKeyPair);

        internal new TokenContractContainer.TokenContractStub TokenContractStub => GetTokenContractTester(BootMinerKeyPair);

        internal new TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub =>
            GetTokenConverterContractTester(BootMinerKeyPair);

        internal new VoteContractContainer.VoteContractStub VoteContractStub => GetVoteContractTester(BootMinerKeyPair);

        internal new ProfitContractContainer.ProfitContractStub ProfitContractStub =>
            GetProfitContractTester(BootMinerKeyPair);

        internal new ElectionContractContainer.ElectionContractStub ElectionContractStub =>
            GetElectionContractTester(BootMinerKeyPair);

        internal new AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub =>
            GetAEDPoSContractTester(BootMinerKeyPair);

        internal new TreasuryContractContainer.TreasuryContractStub TreasuryContractStub =>
            GetTreasuryContractTester(BootMinerKeyPair);

        internal new ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub =>
            GetParliamentAuthContractTester(BootMinerKeyPair);

        internal new EconomicContractContainer.EconomicContractStub EconomicContractStub =>
            GetEconomicContractTester(BootMinerKeyPair);

        internal BasicContractZeroContainer.BasicContractZeroStub GetBasicContractTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal new TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal new TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                keyPair);
        }

        internal new VoteContractContainer.VoteContractStub GetVoteContractTester(ECKeyPair keyPair)
        {
            return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, keyPair);
        }

        internal new ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
        {
            return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
        }

        internal new ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
        {
            return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
        }

        internal AEDPoSContractContainer.AEDPoSContractStub GetAEDPoSContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        internal new TreasuryContractContainer.TreasuryContractStub GetTreasuryContractTester(ECKeyPair keyPair)
        {
            return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
        }

        internal new ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress,
                keyPair);
        }

        internal new EconomicContractContainer.EconomicContractStub GetEconomicContractTester(ECKeyPair keyPair)
        {
            return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
        }
    }
}