using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.MethodCallThreshold;
using AElf.Contracts.TestContract.TransactionFeeCharging;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.TokenHolder;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Standards.ACS1;
using Volo.Abp.Threading;

namespace AElf.Contracts.EconomicSystem.Tests
{
    // ReSharper disable InconsistentNaming
    public class EconomicSystemTestBase : EconomicContractsTestBase
    {
        protected void InitializeContracts()
        {
            DeployAllContracts();
            
            AsyncHelper.RunSync(InitializeParliamentContract);
            AsyncHelper.RunSync(InitializeTreasuryConverter);
            AsyncHelper.RunSync(InitializeElection);
            AsyncHelper.RunSync(InitializeEconomicContract);
            AsyncHelper.RunSync(InitializeToken);
            AsyncHelper.RunSync(InitializeAElfConsensus);
            AsyncHelper.RunSync(InitializeTokenConverter);
            AsyncHelper.RunSync(InitializeTransactionFeeChargingContract);
        }
        
        internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub =>
            GetBasicContractTester(BootMinerKeyPair);

        internal TokenContractImplContainer.TokenContractImplStub TokenContractStub => GetTokenContractTester(BootMinerKeyPair);
        
        internal TokenContractImplContainer.TokenContractImplStub TokenContractImplStub =>
            GetTokenContractImplTester(BootMinerKeyPair);

        internal TokenHolderContractImplContainer.TokenHolderContractImplStub TokenHolderStub =>
            GetTokenHolderTester(BootMinerKeyPair);
        
        internal TokenConverterContractImplContainer.TokenConverterContractImplStub TokenConverterContractStub =>
            GetTokenConverterContractTester(BootMinerKeyPair);

        internal VoteContractImplContainer.VoteContractImplStub VoteContractStub => GetVoteContractTester(BootMinerKeyPair);

        internal ProfitContractImplContainer.ProfitContractImplStub ProfitContractStub =>
            GetProfitContractTester(BootMinerKeyPair);

        internal ElectionContractImplContainer.ElectionContractImplStub ElectionContractStub =>
            GetElectionContractTester(BootMinerKeyPair);

        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub =>
            GetAEDPoSContractTester(BootMinerKeyPair);

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AedPoSContractImplStub =>
            GetAEDPoSImplContractTester(BootMinerKeyPair);

        internal TreasuryContractImplContainer.TreasuryContractImplStub TreasuryContractStub =>
            GetTreasuryContractTester(BootMinerKeyPair);

        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
            GetParliamentContractTester(BootMinerKeyPair);

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            TransactionFeeChargingContractStub => GetTransactionFeeChargingContractTester(BootMinerKeyPair);

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub MethodCallThresholdContractStub =>
            GetMethodCallThresholdContractTester(BootMinerKeyPair);

        internal EconomicContractImplContainer.EconomicContractImplStub EconomicContractStub =>
            GetEconomicContractTester(BootMinerKeyPair);

        internal ConfigurationImplContainer.ConfigurationImplStub ConfigurationContractStub =>
            GetConfigurationContractTester(BootMinerKeyPair);

        internal BasicContractZeroImplContainer.BasicContractZeroImplStub GetBasicContractTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractImplContainer.TokenContractImplStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
        }
        
        internal TokenContractImplContainer.TokenContractImplStub GetTokenContractImplTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, keyPair);
        }

        internal TokenHolderContractImplContainer.TokenHolderContractImplStub GetTokenHolderTester(ECKeyPair keyPair)
        {
            return GetTester<TokenHolderContractImplContainer.TokenHolderContractImplStub>(TokenHolderContractAddress, keyPair);
        }
        
        internal TokenConverterContractImplContainer.TokenConverterContractImplStub GetTokenConverterContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<TokenConverterContractImplContainer.TokenConverterContractImplStub>(TokenConverterContractAddress,
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

        internal AEDPoSContractContainer.AEDPoSContractStub GetAEDPoSContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetAEDPoSImplContractTester(ECKeyPair keyPair)
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

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            GetTransactionFeeChargingContractTester(ECKeyPair keyPair)
        {
            return GetTester<TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub>(
                TransactionFeeChargingContractAddress, keyPair);
        }

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub
            GetMethodCallThresholdContractTester(
                ECKeyPair keyPair)
        {
            return GetTester<MethodCallThresholdContractContainer.MethodCallThresholdContractStub>(
                MethodCallThresholdContractAddress,
                keyPair);
        }

        internal EconomicContractImplContainer.EconomicContractImplStub GetEconomicContractTester(ECKeyPair keyPair)
        {
            return GetTester<EconomicContractImplContainer.EconomicContractImplStub>(EconomicContractAddress, keyPair);
        }
        
        internal ConfigurationImplContainer.ConfigurationImplStub GetConfigurationContractTester(ECKeyPair keyPair)
        {
            return GetTester<ConfigurationImplContainer.ConfigurationImplStub>(ConfigurationAddress, keyPair);
        }

    }
}