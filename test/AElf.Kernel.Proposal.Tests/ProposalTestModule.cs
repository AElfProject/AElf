using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Proposal.Tests
{
    [DependsOn(
        typeof(KernelTestAElfModule),
        typeof(ProposalAElfModule))]
    public class ProposalTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(o =>
                        o.GetAddressByContractNameAsync(It.IsAny<IChainContext>(), It.Is<string>(hash =>
                            hash == ParliamentSmartContractAddressNameProvider.StringName)))
                    .Returns(Task.FromResult(ProposalTestBase.ParliamentContractFakeAddress));

                return mockService.Object;
            });

            context.Services.AddTransient(provider =>
            {
                var proposalTestHelper = context.Services.GetServiceLazy<ProposalTestHelper>().Value;
                var mockService = new Mock<ITransactionReadOnlyExecutionService>();
                mockService.Setup(m =>
                        m.ExecuteAsync(It.IsAny<ChainContext>(),
                            It.IsAny<Transaction>(),
                            It.IsAny<Timestamp>()))
                    .Returns<IChainContext, Transaction, Timestamp>((chainContext, txn, timestamp) =>
                    {
                        var input = ProposalIdList.Parser.ParseFrom(txn.Params);
                        if (txn.MethodName == nameof(ParliamentContractContainer.ParliamentContractStub
                            .GetNotVotedProposals))
                        {
                            var notApprovedProposalIdList = proposalTestHelper.GetNotVotedProposalIdList(input);
                            return Task.FromResult(new TransactionTrace
                            {
                                ExecutionStatus = ExecutionStatus.Executed,
                                ReturnValue = new ProposalIdList
                                {
                                    ProposalIds = {notApprovedProposalIdList.ProposalIds.Intersect(input.ProposalIds)}
                                }.ToByteString()
                            });
                        }

                        if (txn.MethodName == nameof(ParliamentContractContainer.ParliamentContractStub
                            .GetNotVotedPendingProposals))
                        {
                            var notApprovedPendingProposalIdList =
                                proposalTestHelper.GetNotVotedPendingProposalIdList(input);
                            return Task.FromResult(new TransactionTrace
                            {
                                ExecutionStatus = ExecutionStatus.Executed,
                                ReturnValue = new ProposalIdList
                                {
                                    ProposalIds =
                                        {notApprovedPendingProposalIdList.ProposalIds.Intersect(input.ProposalIds)}
                                }.ToByteString()
                            });
                        }

                        return null;
                    });

                return mockService.Object;
            });
        }
    }
}