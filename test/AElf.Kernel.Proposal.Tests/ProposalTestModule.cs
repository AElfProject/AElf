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
                        o.GetAddressByContractName(It.Is<Hash>(hash =>
                            hash == ParliamentSmartContractAddressNameProvider.Name)))
                    .Returns(ProposalTestBase.ParliamentContractFakeAddress);

                return mockService.Object;
            });
            
            context.Services.AddTransient(provider =>
            {
                var proposalTestHelper = context.Services.GetServiceLazy<ProposalTestHelper>().Value;
                var mockService = new Mock<ITransactionReadOnlyExecutionService>();
                mockService.Setup(m =>
                        m.ExecuteAsync(It.IsAny<ChainContext>(),
                            It.Is<Transaction>(tx =>
                                tx.MethodName == nameof(ParliamentContractContainer.ParliamentContractStub
                                    .GetNotVotedProposals)),
                            It.IsAny<Timestamp>()))
                    .Returns<IChainContext, Transaction, Timestamp>((chainContext, txn, timestamp) =>
                    {
                        var input = ProposalIdList.Parser.ParseFrom(txn.Params);
                        return Task.FromResult(new TransactionTrace
                        {
                            ExecutionStatus = ExecutionStatus.Executed,
                            ReturnValue = proposalTestHelper.GetNotVotedProposalIdList(input).ToByteString()
                        });
                    });

                return mockService.Object;
            });
        }
    }
}