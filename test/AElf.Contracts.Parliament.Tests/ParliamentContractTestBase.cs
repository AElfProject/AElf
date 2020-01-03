using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;
using SampleECKeyPairs = AElf.Contracts.TestKit.SampleECKeyPairs;

namespace AElf.Contracts.Parliament
{
    public class ParliamentContractTestBase : TestKit.ContractTestBase<ParliamentContractTestAElfModule>
    {
        protected const int MinersCount = 3;
        protected const int MiningInterval = 4000;
        protected DateTime BlockchainStartTime => DateTime.Parse("2019-01-01 00:00:00.000").ToUniversalTime();

        protected byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        protected byte[] ParliamentCode => Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
        protected byte[] DPoSConsensusCode => Codes.Single(kv => kv.Key.Contains("Consensus.AEDPoS")).Value;

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[10];
        protected ECKeyPair TesterKeyPair => SampleECKeyPairs.KeyPairs[MinersCount + 1];
        protected List<ECKeyPair> InitialMinersKeyPairs => SampleECKeyPairs.KeyPairs.Take(MinersCount).ToList();
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected Address Tester => Address.FromPublicKey(TesterKeyPair.PublicKey);

        protected Address TokenContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }
        protected Address ParliamentContractAddress { get; set; }
        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

        protected IBlockTimeProvider BlockTimeProvider =>
            Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();

        internal BasicContractZeroContainer.BasicContractZeroStub BasicContractStub { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractStub ConsensusContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub { get; set; }
        

        protected void InitializeContracts()
        {
            //get basic stub
            BasicContractStub =
                GetContractZeroTester(DefaultSenderKeyPair);
            
            //deploy Parliament contract
            ParliamentContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    ParliamentCode,
                    ParliamentSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair
                ));
            ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
            //deploy token contract
            TokenContractAddress = AsyncHelper.RunSync(() =>
                DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name,
                    DefaultSenderKeyPair));
            TokenContractStub = GetTokenContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeTokenAsync());

            ConsensusContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                DPoSConsensusCode,
                ConsensusSmartContractAddressNameProvider.Name,
                DefaultSenderKeyPair));
            ConsensusContractStub = GetConsensusContractTester(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeConsensusAsync());
        }


        internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
        }

        internal AEDPoSContractContainer.AEDPoSContractStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
        }

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                keyPair);
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

        private async Task InitializeConsensusAsync()
        {
            await ConsensusContractStub.InitialAElfConsensusContract.SendAsync(new InitialAElfConsensusContractInput
            {
                IsTermStayOne = true
            });
            var minerList = new MinerList
                {Pubkeys = {InitialMinersKeyPairs.Select(m => m.PublicKey.ToHex().ToByteString())}};
            await ConsensusContractStub.FirstRound.SendAsync(
                minerList.GenerateFirstRoundOfNewTerm(MiningInterval, BlockchainStartTime));
        }
        
        internal async Task InitializeParliamentContracts()
        {
            await ParliamentContractStub.Initialize.SendAsync(new InitializeInput());
        }
    }

    public class ParliamentContractPrivilegeTestBase : TestBase.ContractTestBase<ParliamentContractPrivilegeTestAElfModule>
    {
        protected Address ParliamentAddress;
        protected Address TokenContractAddress;

        protected long TotalSupply;
        protected long BalanceOfStarter;
        protected new ContractTester<ParliamentContractPrivilegeTestAElfModule> Tester;


        public ParliamentContractPrivilegeTestBase()
        {
            var mainChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            var chainId = ChainHelper.GetChainId(1);
            Tester = new ContractTester<ParliamentContractPrivilegeTestAElfModule>(chainId,SampleECKeyPairs.KeyPairs[1]);
            AsyncHelper.RunSync(() =>
                Tester.InitialChainAsyncWithAuthAsync(Tester.GetSideChainSystemContract(
                    Tester.GetCallOwnerAddress(),
                    mainChainId,"STA",out TotalSupply,Tester.GetCallOwnerAddress())));
            ParliamentAddress = Tester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        }

        internal TransferInput TransferInput(Address address)
        {
            var transferInput = new TransferInput
            {
                Symbol = "ELF",
                Amount = 100,
                To = address,
                Memo = "Transfer"
            };
            return transferInput;
        }

        internal CreateProposalInput CreateProposalInput(IMessage input, Address organizationAddress)
        {
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                Params = input.ToByteString(),
                ToAddress = TokenContractAddress,
                OrganizationAddress = organizationAddress
            };
            return createProposalInput;
        }

        internal CreateProposalInput CreateParliamentProposalInput(IMessage input, Address organizationAddress)
        {
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(ParliamentContractContainer.ParliamentContractStub.ChangeOrganizationProposerWhiteList),
                ToAddress = ParliamentAddress,
                Params = input.ToByteString(),
                ExpiredTime =  DateTime.UtcNow.AddDays(1).ToTimestamp(),
                OrganizationAddress = organizationAddress
            };
            return createProposalInput;
        }
    }
}