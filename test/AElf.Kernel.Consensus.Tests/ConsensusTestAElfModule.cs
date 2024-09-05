using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.Standards.ACS4;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus;

[DependsOn(
    typeof(CoreConsensusAElfModule),
    typeof(SmartContractAElfModule),
    typeof(KernelCoreWithChainTestAElfModule)
)]
public class ConsensusTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        services.AddSingleton<IConsensusTestHelper, ConsensusTestHelper>();

        services.AddSingleton<IConsensusScheduler, MockConsensusScheduler>();
        services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();

        services.AddTransient(provider =>
        {
            var mockService = new Mock<ITriggerInformationProvider>();
            mockService.Setup(m => m.GetTriggerInformationForConsensusCommand(It.IsAny<BytesValue>()))
                .Returns(new BytesValue());
            mockService.Setup(m => m.GetTriggerInformationForBlockHeaderExtraData(It.IsAny<BytesValue>()))
                .Returns(new BytesValue());
            mockService.Setup(m => m.GetTriggerInformationForConsensusTransactions(It.IsAny<IChainContext>(), It.IsAny<BytesValue>()))
                .Returns(new BytesValue());

            return mockService.Object;
        });

        services.AddTransient(provider =>
        {
            var mockService = new Mock<ISmartContractAddressService>();
            mockService.Setup(m => m.GetAddressByContractNameAsync(It.IsAny<IChainContext>(), It.IsAny<string>()))
                .Returns(Task.FromResult(SampleAddress.AddressList[0]));

            return mockService.Object;
        });

        services.AddTransient(provider =>
        {
            var mockService = new Mock<IConsensusExtraDataExtractor>();
            mockService.Setup(m => m.ExtractConsensusExtraData(It.Is<BlockHeader>(o => o.Height == 9)))
                .Returns(ByteString.Empty);
            mockService.Setup(m => m.ExtractConsensusExtraData(It.Is<BlockHeader>(o => o.Height != 9)))
                .Returns(ByteString.CopyFromUtf8("test"));
            return mockService.Object;
        });

        //mock consensus service transaction execution result
        services.AddTransient(provider =>
        {
            var mockService = new Mock<ITransactionReadOnlyExecutionService>();
            mockService.Setup(m =>
                    m.ExecuteAsync(It.IsAny<ChainContext>(),
                        It.Is<Transaction>(tx => tx.MethodName == "GetConsensusCommand"), It.IsAny<Timestamp>()))
                .Returns(Task.FromResult(new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    ReturnValue = ByteString.CopyFrom(new ConsensusCommand
                    {
                        ArrangedMiningTime = TimestampHelper.GetUtcNow(),
                        Hint = new AElfConsensusHint { Behaviour = AElfConsensusBehaviour.Nothing }.ToByteString(),
                        LimitMillisecondsOfMiningBlock = 400
                    }.ToByteArray())
                }));

            mockService.Setup(m =>
                    m.ExecuteAsync(It.IsAny<ChainContext>(),
                        It.Is<Transaction>(tx =>
                            tx.MethodName.Contains("ValidateConsensus") && tx.Params == ByteString.Empty),
                        It.IsAny<Timestamp>()))
                .Returns(Task.FromResult(new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    ReturnValue = ByteString.CopyFrom(new ValidationResult
                    {
                        Success = false,
                        Message = "Parameter is not valid"
                    }.ToByteArray())
                }));

            mockService.Setup(m =>
                    m.ExecuteAsync(It.IsAny<ChainContext>(),
                        It.Is<Transaction>(tx =>
                            tx.MethodName.Contains("ValidateConsensus") && tx.Params != ByteString.Empty),
                        It.IsAny<Timestamp>()))
                .Returns(Task.FromResult(new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    ReturnValue = ByteString.CopyFrom(new ValidationResult
                    {
                        Success = true,
                        Message = "Validate success"
                    }.ToByteArray())
                }));

            mockService.Setup(m =>
                    m.ExecuteAsync(It.IsAny<ChainContext>(),
                        It.Is<Transaction>(tx => tx.MethodName == "GetConsensusExtraData"),
                        It.IsAny<Timestamp>()))
                .Returns(Task.FromResult(new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    ReturnValue = ByteString.CopyFrom(new AElfConsensusHeaderInformation
                    {
                        Behaviour = AElfConsensusBehaviour.Nothing,
                        Round = new Round()
                    }.ToBytesValue().ToByteArray())
                }));

            mockService.Setup(m =>
                    m.ExecuteAsync(It.IsAny<ChainContext>(),
                        It.Is<Transaction>(tx => tx.MethodName == "GenerateConsensusTransactions"),
                        It.IsAny<Timestamp>()))
                .Returns(Task.FromResult(new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    ReturnValue = ByteString.CopyFrom(new TransactionList
                    {
                        Transactions =
                        {
                            new Transaction
                            {
                                From = SampleAddress.AddressList[0],
                                To = SampleAddress.AddressList[1],
                                MethodName = "NextTerm",
                                Params = ByteString.Empty
                            }
                        }
                    }.ToByteArray())
                }));

            return mockService.Object;
        });

        services.AddTransient<IBlockValidationProvider, ConsensusValidationProvider>();
    }
}