using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.Configuration;
using AElf.Kernel.Proposal;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.Standards.ACS0;
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
        context.Services.AddSingleton<IBlockValidationProvider, CodeCheckValidationProvider>();
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
                    if (txn.MethodName == nameof(ParliamentContractContainer.ParliamentContractStub
                            .GetReleaseThresholdReachedProposals))
                    {
                        var input = ProposalIdList.Parser.ParseFrom(txn.Params);
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
                        var input = ProposalIdList.Parser.ParseFrom(txn.Params);
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

                    if (txn.MethodName == nameof(ACS0Container.ACS0Stub.GetContractInfo))
                    {
                        var address = Address.Parser.ParseFrom(txn.Params);
                        if (address == SampleAddress.AddressList.First())
                        {
                            return Task.FromResult(new TransactionTrace
                            {
                                ExecutionStatus = ExecutionStatus.Executed,
                                ReturnValue = new ContractInfo()
                                {
                                    Author = address,
                                    Category = 1,
                                }.ToByteString()
                            });
                        }
                        if (address == SampleAddress.AddressList.Last())
                        {
                            return Task.FromResult(new TransactionTrace
                            {
                                ExecutionStatus = ExecutionStatus.Executed,
                                ReturnValue = new ContractInfo()
                                {
                                    Author = address,
                                    Category = 0,
                                }.ToByteString()
                            });
                        }
                        return Task.FromResult(new TransactionTrace
                        {
                            ExecutionStatus = ExecutionStatus.Executed,
                            ReturnValue = new ContractInfo().ToByteString()
                        });
                    }

                    if (txn.MethodName == nameof(ACS0Container.ACS0Stub.GetContractCodeHashListByDeployingBlockHeight))
                    {
                        var height = Int64Value.Parser.ParseFrom(txn.Params).Value;
                        switch (height)
                        {
                            case 2:
                                return Task.FromResult(new TransactionTrace
                                {
                                    ExecutionStatus = ExecutionStatus.Executed,
                                    ReturnValue = new ContractCodeHashList().ToByteString()
                                });
                            case 3:
                            case 4:
                                return Task.FromResult(new TransactionTrace
                                {
                                    ExecutionStatus = ExecutionStatus.Executed,
                                    ReturnValue = new ContractCodeHashList{Value = { HashHelper.ComputeFrom(height) }}.ToByteString()
                                });
                        }
                    }
                    
                    if (txn.MethodName == nameof(ACS0Container.ACS0Stub.GetSmartContractRegistrationByCodeHash))
                    {
                        var codeHash = Hash.Parser.ParseFrom(txn.Params);
                        if (codeHash == HashHelper.ComputeFrom(3L))
                        {
                            return Task.FromResult(new TransactionTrace
                            {
                                ExecutionStatus = ExecutionStatus.Executed,
                                ReturnValue = new SmartContractRegistration
                                {
                                    Category = CodeCheckConstant.SuccessAudit
                                }.ToByteString()
                            });
                        }
                        
                        if (codeHash == HashHelper.ComputeFrom(4L))
                        {
                            return Task.FromResult(new TransactionTrace
                            {
                                ExecutionStatus = ExecutionStatus.Executed,
                                ReturnValue = new SmartContractRegistration
                                {
                                    Category = CodeCheckConstant.FailAudit
                                }.ToByteString()
                            });
                        }
                    }

                    return null;
                });

            return mockService.Object;
        });
    }
}