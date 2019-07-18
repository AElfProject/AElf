using System;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.Genesis
{
    public class
        AuthorityNotRequiredBasicContractZeroTestBase : ContractTestBase<
            AuthorityNotRequiredBasicContractZeroTestModule>
    {
        protected ISmartContractAddressService ContractAddressService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();

        protected Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

        internal ACS0Container.ACS0Stub DefaultTester =>
            GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, DefaultSenderKeyPair);

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs.First();
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected ECKeyPair AnotherUserKeyPair => SampleECKeyPairs.KeyPairs.Last();
        protected Address AnotherUser => Address.FromPublicKey(AnotherUserKeyPair.PublicKey);

        internal ACS0Container.ACS0Stub AnotherTester =>
            GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, AnotherUserKeyPair);
    }

    public class BasicContractZeroTestBase : TestBase.ContractTestBase<BasicContractZeroTestAElfModule>
    {
        protected Address ParliamentAddress;
        protected Address BasicContractZeroAddress;
        protected Address TokenContractAddress;

        protected long _totalSupply;
        protected long _balanceOfStarter;

        public BasicContractZeroTestBase()
        {
            AsyncHelper.RunSync(() =>
                Tester.InitialChainAsyncWithAuthAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _totalSupply,
                    out _,
                    out _balanceOfStarter)));
            BasicContractZeroAddress = Tester.GetZeroContractAddress();
            ParliamentAddress = Tester.GetContractAddress(ParliamentAuthSmartContractAddressNameProvider.Name);
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        }
        
        protected async Task<TransactionResult> ApproveWithMinersAsync(Hash proposalId)
        {
            var tester0 = Tester.CreateNewContractTester(Tester.InitialMinerList[0]);
            await tester0.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                });
            var tester1 = Tester.CreateNewContractTester(Tester.InitialMinerList[1]);
            var txResult = await tester1.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                });

            return txResult;
        }

        protected async Task<Hash> CreateProposalAsync(string methodName, IMessage input)
        {
            var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = methodName,
                    ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                    Params = input.ToByteString(),
                    ToAddress = BasicContractZeroAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }

        protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId)
        {
            var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release),proposalId);
            return transactionResult;
        }
    }
}