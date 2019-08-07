using System.Collections.Generic;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestBase;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.ConfigurationContract.Tests
{
    public class ConfigurationContractTestBase : ContractTestBase<ConfigurationContractTestAElfModule>
    {
        protected Address ParliamentAddress;
        protected Address ConfigurationContractAddress;

        protected long _totalSupply;
        protected long _balanceOfStarter;

        public ConfigurationContractTestBase()
        {
            AsyncHelper.RunSync(() =>
                Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _totalSupply,
                    out _,
                    out _balanceOfStarter)));
            ParliamentAddress = Tester.GetContractAddress(ParliamentAuthSmartContractAddressNameProvider.Name);
            ConfigurationContractAddress =
                Tester.GetContractAddress(ConfigurationSmartContractAddressNameProvider.Name);
        }

        protected async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress,
            string methodName, IMessage input)
        {
            return await Tester.ExecuteContractWithMiningAsync(contractAddress, methodName, input);
        }

        protected async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
            ECKeyPair ecKeyPair, IMessage input)
        {
            return ecKeyPair == null
                ? await Tester.GenerateTransactionAsync(contractAddress, methodName, input)
                : await Tester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
        }

        internal Int32Value SetBlockTransactionLimitRequest(int amount)
        {
            return new Int32Value {Value = amount};
        }

        internal async Task<Hash> SetBlockTransactionLimitProposalAsync(int amount)
        {
            var createProposalInput = SetBlockTransactionLimitRequest(amount);
            var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = "SetBlockTransactionLimit",
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = createProposalInput.ToByteString(),
                    ToAddress = ConfigurationContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }

        protected async Task ApproveWithMinersAsync(Hash proposalId)
        {
            var approveTransaction1 = await GenerateTransactionAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), Tester.InitialMinerList[0],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                });
            var approveTransaction2 = await GenerateTransactionAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), Tester.InitialMinerList[1],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                });

            // Mine a block with given normal txs and system txs.
            await Tester.MineAsync(new List<Transaction> {approveTransaction1, approveTransaction2});
        }

        protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId)
        {
            var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release), proposalId);
            return transactionResult;
        }
        
        internal async Task<Hash> SetTransactionOwnerAddressProposalAsync(Address address)
        {
            var createProposalInput = address;
            var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = "SetTransactionOwnerAddress",
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = createProposalInput.ToByteString(),
                    ToAddress = ConfigurationContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }
    }
}