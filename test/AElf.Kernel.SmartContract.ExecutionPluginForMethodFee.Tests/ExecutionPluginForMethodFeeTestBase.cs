using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class ExecutionPluginForMethodFeeTestBase : ContractTestBase<ExecutionPluginForMethodFeeTestModule>
    {
    }

    public class ExecutionPluginForMethodFeeWithForkTestBase : Contracts.TestBase.ContractTestBase<
        ExecutionPluginForMethodFeeWithForkTestModule>
    {
        protected readonly Address TokenContractAddress;
        private readonly Address _parliamentAddress;

        protected ExecutionPluginForMethodFeeWithForkTestBase()
        {
            AsyncHelper.RunSync(() =>
                Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress(), out _, out _,
                    out _)));
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            _parliamentAddress = Tester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
            var amount = 1000_00000000;
            AsyncHelper.RunSync(() =>
                IssueNativeTokenAsync(Address.FromPublicKey(Tester.InitialMinerList[0].PublicKey), amount));
            AsyncHelper.RunSync(() =>
                IssueNativeTokenAsync(Address.FromPublicKey(Tester.InitialMinerList[2].PublicKey), amount));
        }

        private async Task IssueNativeTokenAsync(Address address,long amount)
        {
            await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
                {
                    Amount = amount,
                    Memo = Guid.NewGuid().ToString(),
                    Symbol = "ELF",
                    To = address
                });
        }

        protected async Task SetMethodFeeWithProposalAsync(ByteString methodFee)
        {
            var proposal = await Tester.ExecuteContractWithMiningAsync(_parliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = nameof(MethodFeeProviderContractContainer.MethodFeeProviderContractStub.SetMethodFee),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = methodFee,
                    ToAddress = TokenContractAddress,
                    OrganizationAddress = await GetParliamentDefaultOrganizationAddressAsync()
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);
        }

        private async Task<Address> GetParliamentDefaultOrganizationAddressAsync()
        {
            var organizationAddress = Address.Parser.ParseFrom((await Tester.ExecuteContractWithMiningAsync(
                    _parliamentAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.GetDefaultOrganizationAddress),
                    new Empty()))
                .ReturnValue);
            return organizationAddress;
        }
        private async Task ApproveWithMinersAsync(Hash proposalId)
        {
            var approveTransaction1 = await GenerateTransactionAsync(_parliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.Approve), Tester.InitialMinerList[0],
                proposalId);
            var approveTransaction2 = await GenerateTransactionAsync(_parliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.Approve), Tester.InitialMinerList[1],
                proposalId);
            var approveTransaction3 = await GenerateTransactionAsync(_parliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.Approve), Tester.InitialMinerList[2],
                proposalId);

            // Mine a block with given normal txs and system txs.
            await Tester.MineAsync(
                new List<Transaction> {approveTransaction1, approveTransaction2, approveTransaction3});
        }
        
        private async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
            ECKeyPair ecKeyPair, IMessage input)
        {
            return ecKeyPair == null
                ? await Tester.GenerateTransactionAsync(contractAddress, methodName, input)
                : await Tester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
        }

        private async Task ReleaseProposalAsync(Hash proposalId)
        {
            await Tester.ExecuteContractWithMiningAsync(_parliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.Release), proposalId);
        }
    }

}