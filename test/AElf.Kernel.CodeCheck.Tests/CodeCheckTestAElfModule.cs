using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Kernel.Configuration;
using AElf.Kernel.Proposal;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.CodeCheck.Tests;

[DependsOn(
    typeof(KernelTestAElfModule),
    typeof(CodeCheckAElfModule))]
public class CodeCheckTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        //IProposalProvider is used to check code check ProcessAsync
        context.Services.RemoveAll<IProposalProvider>();
        context.Services.AddSingleton<IProposalProvider, ProposalProvider>();
        context.Services.AddSingleton<ILogEventProcessor, CodeCheckRequiredLogEventProcessor>();
        Configure<CodeCheckOptions>(options => { options.CodeCheckEnabled = true; });
        context.Services.AddTransient(provider =>
        {
            var mockService = new Mock<IConfigurationService>();
            mockService.Setup(m =>
                    m.GetConfigurationDataAsync(It.IsAny<string>(),
                        It.IsAny<ChainContext>()))
                .Returns<string, ChainContext>((configurationName, chainContext) =>
                {
                    if (configurationName == CodeCheckConstant.RequiredAcsInContractsConfigurationName)
                        return Task.FromResult(new RequiredAcsInContracts
                        {
                            AcsList = { CodeCheckConstant.Acs1, CodeCheckConstant.Acs2 },
                            RequireAll = CodeCheckConstant.IsRequireAllAcs
                        }.ToByteString());
                    return null;
                });
            return mockService.Object;
        });
        
        context.Services.AddTransient(provider =>
        {
            var mockService = new Mock<ISmartContractAddressService>();
            mockService.Setup(o =>
                    o.GetAddressByContractNameAsync(It.IsAny<IChainContext>(), It.Is<string>(hash =>
                        hash == ParliamentSmartContractAddressNameProvider.StringName)))
                .Returns(Task.FromResult(CodeCheckTestBase.ParliamentContractFakeAddress));

            mockService.Setup(o =>
                    o.GetZeroSmartContractAddress())
                .Returns(CodeCheckTestBase.ZeroContractFakeAddress);
            
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
                            .GetReleaseThresholdReachedProposals))
                    {
                        var notApprovedProposalIdList = proposalTestHelper.GetReleaseThresholdReachedProposals(input);
                        return Task.FromResult(new TransactionTrace
                        {
                            ExecutionStatus = ExecutionStatus.Executed,
                            ReturnValue = new ProposalIdList
                            {
                                ProposalIds = { notApprovedProposalIdList.ProposalIds.Intersect(input.ProposalIds) }
                            }.ToByteString()
                        });
                    }

                    if (txn.MethodName == nameof(ParliamentContractContainer.ParliamentContractStub
                            .GetAvailableProposals))
                    {
                        var notApprovedPendingProposalIdList =
                            proposalTestHelper.GetAvailableProposals(input);
                        return Task.FromResult(new TransactionTrace
                        {
                            ExecutionStatus = ExecutionStatus.Executed,
                            ReturnValue = new ProposalIdList
                            {
                                ProposalIds =
                                    { notApprovedPendingProposalIdList.ProposalIds.Intersect(input.ProposalIds) }
                            }.ToByteString()
                        });
                    }

                    return null;
                });

            return mockService.Object;
        });
    }
}