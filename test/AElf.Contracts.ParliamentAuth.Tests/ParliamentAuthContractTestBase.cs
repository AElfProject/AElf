using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthContractTestBase : ContractTestBase<ParliamentAuthContractTestAElfModule>
    {
        protected const int MinersCount = 3;
        protected const int MiningInterval = 4000;
        protected DateTime BlockchainStartTime => DateTime.Parse("2019-01-01 00:00:00.000").ToUniversalTime();
        
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected ECKeyPair TesterKeyPair => SampleECKeyPairs.KeyPairs[MinersCount+1];
        protected List<ECKeyPair> InitialMinersKeyPairs => SampleECKeyPairs.KeyPairs.Take(MinersCount).ToList();
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address Tester => Address.FromPublicKey(TesterKeyPair.PublicKey);
        
        protected Address TokenContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }
        protected Address ParliamentAuthContractAddress { get; set; }
        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        
        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal ConsensusContractContainer.ConsensusContractStub ConsensusContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractStub OtherParliamentAuthContractStub { get; set; }
        
        protected void InitializeContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy parliamentAuth contract
            ParliamentAuthContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ParliamentAuthContract).Assembly.Location)),
                        Name =  Hash.FromString("AElf.ContractNames.ParliamentAuth"),
                        TransactionMethodCallList = GenerateParliamentAuthInitializationCallList()
                    })).Output;
            ParliamentAuthContractStub = GetParliamentAuthContractTester(DefaultSenderKeyPair);
            
            var otherParliamentAuthContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySmartContract.SendAsync(
                    new ContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ParliamentAuthContract).Assembly.Location)),
                    })).Output;
            OtherParliamentAuthContractStub = GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(otherParliamentAuthContractAddress, DefaultSenderKeyPair);
            
            //deploy token contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                BasicContractZeroStub.DeploySystemSmartContract.SendAsync(
                    new SystemContractDeploymentInput
                    {
                        Category = KernelConstants.CodeCoverageRunnerCategory,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)),
                        Name = TokenSmartContractAddressNameProvider.Name,
                        TransactionMethodCallList = GenerateTokenInitializationCallList()
                    })).Output;
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
            
            ConsensusContractAddress = AsyncHelper.RunSync(() => GetContractZeroTester(DefaultSenderKeyPair)
                .DeploySystemSmartContract.SendAsync(new SystemContractDeploymentInput
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ConsensusContract).Assembly.Location)),
                    Name = ConsensusSmartContractAddressNameProvider.Name,
                    TransactionMethodCallList = GenerateConsensusInitializationCallList()
                })).Output;
            ConsensusContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
        }
        
        
        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }
        
        internal ConsensusContractContainer.ConsensusContractStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<ConsensusContractContainer.ConsensusContractStub>(ConsensusContractAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress, keyPair);
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateParliamentAuthInitializationCallList()
        {
            var parliamentMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentMethodCallList.Add(nameof(ParliamentAuthContract.Initialize),
                new ParliamentAuthInitializationInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name
                });

            return parliamentMethodCallList;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            const string symbol = "ELF";
            const long totalSupply = 100_000_000;
            var tokenContractCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = totalSupply,
                Issuer = DefaultSender,
            });

            //issue default user
            tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
            {
                Symbol = symbol,
                Amount = totalSupply - 20 * 100_000L,
                To = DefaultSender,
                Memo = "Issue token to default user.",
            });
            return tokenContractCallList;
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateConsensusInitializationCallList()
        {
            var consensusMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialConsensus),
                InitialMinersKeyPairs.Select(m => m.PublicKey.ToHex()).ToList().ToMiners().GenerateFirstRoundOfNewTerm(
                    MiningInterval, BlockchainStartTime));
            
            return consensusMethodCallList;
        }
    }
}