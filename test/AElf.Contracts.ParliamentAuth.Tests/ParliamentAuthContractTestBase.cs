using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthContractTestBase : ContractTestBase<ParliamentAuthContractTestAElfModule>
    {
        protected const int MinersCount = 3;
        protected const int MiningInterval = 4000;
        protected DateTime BlockchainStartTime => DateTime.Parse("2019-01-01 00:00:00.000").ToUniversalTime();

        protected byte[] TokenContractCoe => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        protected byte[] ParliamentAuthCode => Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
        protected byte[] DPoSConsensusCode => Codes.Single(kv => kv.Key.Contains("Consensus.AEDPoS")).Value;
        
        protected DateTime BlockchainStartTimestamp => DateTime.Parse("2019-01-01 00:00:00.000").ToUniversalTime();

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected ECKeyPair TesterKeyPair => SampleECKeyPairs.KeyPairs[MinersCount + 1];
        protected List<ECKeyPair> InitialMinersKeyPairs => SampleECKeyPairs.KeyPairs.Take(MinersCount).ToList();
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address Tester => Address.FromPublicKey(TesterKeyPair.PublicKey);

        protected Address TokenContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }
        protected Address ParliamentAuthContractAddress { get; set; }
        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

//        internal BasicContractZeroContainer BasicContractZeroStub { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractStub ConsensusContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub { get; set; }

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub OtherParliamentAuthContractStub
        {
            get;
            set;
        }

        protected void InitializeContracts()
        {
//            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy parliamentAuth contract
            ParliamentAuthContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ParliamentAuthCode,
                    Hash.FromString("AElf.ContractNames.ParliamentAuth"),
                    DefaultSenderKeyPair
                ));
            ParliamentAuthContractStub = GetParliamentAuthContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializationParliamentAuth());

            var otherParliamentAuthContractAddress = AsyncHelper.RunSync(() =>
                DeployContractAsync(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ParliamentAuthCode,
                    DefaultSenderKeyPair));
            OtherParliamentAuthContractStub = GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(otherParliamentAuthContractAddress, DefaultSenderKeyPair);
            
            //deploy token contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(

                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCoe,
                    TokenSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair));
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeToken());

            ConsensusContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                DPoSConsensusCode,
                ConsensusSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair));
            ConsensusContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeConsensus());
        }
        
        
        internal ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
        }

        internal AEDPoSContractContainer.AEDPoSContractStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress,
                keyPair);
        }

        private async Task InitializationParliamentAuth()
        {
            await ParliamentAuthContractStub.Initialize.SendAsync(new Empty());
        }
//        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateParliamentAuthInitializationCallList()
//        {
//            var parliamentMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
//            parliamentMethodCallList.Add(nameof(ParliamentAuthContractStub.Initialize),
//                new ParliamentAuthInitializationInput
//                {
//                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name
//                });
//
//            return parliamentMethodCallList;
//        }

        private async Task InitializeToken()
        {
            const string symbol = "ELF";
            const long totalSupply = 100_000_000;
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = DefaultSender,
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user.",
            });
        }
//        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
//        {
//            const string symbol = "ELF";
//            const long totalSupply = 100_000_000;
//            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
//            tokenContractCallList.Add(nameof(TokenContractStub.CreateNativeToken), new CreateNativeTokenInput
//            {
//                Symbol = symbol,
//                Decimals = 2,
//                IsBurnable = true,
//                TokenName = "elf token",
//                TotalSupply = totalSupply,
//                Issuer = DefaultSender,
//            });
//
//            //issue default user
//            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
//            {
//                Symbol = symbol,
//                Amount = totalSupply - 20 * 100_000L,
//                To = DefaultSender,
//                Memo = "Issue token to default user.",
//            });
//            return tokenContractCallList;
//        }
        private async Task InitializeConsensus()
        {
            await ConsensusContractStub.InitialAElfConsensusContract.SendAsync(new InitialAElfConsensusContractInput
            {
                IsTermStayOne = true
            });
            var minerList = new MinerList {PublicKeys = {InitialMinersKeyPairs.Select(m => m.PublicKey.ToHex().ToByteString())}};
            await ConsensusContractStub.FirstRound.SendAsync(minerList.GenerateFirstRoundOfNewTerm(MiningInterval, BlockchainStartTime));
        }
//        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateConsensusInitializationCallList()
//        {
//            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
//            consensusMethodCallList.Add(nameof(AEDPoSContract.InitialAElfConsensusContract),
//                new InitialAElfConsensusContractInput
//                {
//                    IsTermStayOne = true
//                });
//            consensusMethodCallList.Add(nameof(AEDPoSContract.FirstRound),
//                InitialMinersKeyPairs.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateFirstRoundOfNewTerm(
//                    MiningInterval, BlockchainStartTime));
//            
//            return consensusMethodCallList;
//        }
    }
}