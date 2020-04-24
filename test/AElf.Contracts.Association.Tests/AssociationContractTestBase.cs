using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.Parliament.InitializeInput;

namespace AElf.Contracts.Association
{
    public class AssociationContractTestBase<T> : ContractTestBase<T> where T : ContractTestModule
    {
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected ECKeyPair Reviewer1KeyPair => SampleECKeyPairs.KeyPairs[1];
        protected ECKeyPair Reviewer2KeyPair => SampleECKeyPairs.KeyPairs[2];
        protected ECKeyPair Reviewer3KeyPair => SampleECKeyPairs.KeyPairs[3];
        protected Address Reviewer1 => Address.FromPublicKey(Reviewer1KeyPair.PublicKey);
        protected Address Reviewer2 => Address.FromPublicKey(Reviewer2KeyPair.PublicKey);
        protected Address Reviewer3 => Address.FromPublicKey(Reviewer3KeyPair.PublicKey);
        
        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(5).ToList();

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address TokenContractAddress { get; set; }
        protected Address AssociationContractAddress { get; set; }
        protected Address ParliamentContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal AssociationContractContainer.AssociationContractStub AssociationContractStub { get; set; }

        internal AssociationContractContainer.AssociationContractStub AnotherChainAssociationContractStub { get; set; }
        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub { get; set; }
        
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }
        private byte[] AssociationContractCode => Codes.Single(kv => kv.Key.Contains("Association")).Value;
        private byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        private byte[] ParliamentContractCode => Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
        private byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("Consensus.AEDPoS")).Value;

        protected void DeployContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy Association contract
            AssociationContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    AssociationContractCode,
                    AssociationSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair
                ));

            AssociationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair));
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeTokenAsync());

            ParliamentContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ParliamentContractCode,
                    ParliamentSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair
                ));
            ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeParliamentContract());

            ConsensusContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ConsensusContractCode,
                    HashHelper.ComputeFromString("AElf.ContractNames.Consensus"),
                    DefaultSenderKeyPair
                ));
            AEDPoSContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeAElfConsensus());
        }
        
        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal AssociationContractContainer.AssociationContractStub GetAssociationContractTester(ECKeyPair keyPair)
        {
            return GetTester<AssociationContractContainer.AssociationContractStub>(AssociationContractAddress, keyPair);
        }

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress, keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }
        private async Task InitializeTokenAsync()
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

        private async Task InitializeParliamentContract()
        {
            var initializeResult = await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
            {
                PrivilegedProposer = DefaultSender,
                ProposerAuthorityRequired = true
            });
            if (!string.IsNullOrEmpty(initializeResult.TransactionResult.Error))
            {
                throw new Exception(initializeResult.TransactionResult.Error);
            }
        }

        protected async Task InitializeAElfConsensus()
        {
            {
                var result = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                    new InitialAElfConsensusContractInput
                    {
                        PeriodSeconds = 604800L,
                        MinerIncreaseInterval = 31536000
                    });
            }
            {
                var result = await AEDPoSContractStub.FirstRound.SendAsync(
                    new MinerList
                    {
                        Pubkeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()));
            }
        }
    }
}