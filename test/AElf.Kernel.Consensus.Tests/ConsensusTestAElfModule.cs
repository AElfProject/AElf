using System.Threading.Tasks;
using Acs4;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.OS;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus
{
    [DependsOn(
        typeof(CoreAElfModule),
        typeof(OSCoreTestAElfModule),
        typeof(KernelAElfModule),
        typeof(ConsensusAElfModule),
        typeof(CoreKernelAElfModule)
    )]
    public class ConsensusTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddSingleton<IBlockchainService>(provider =>
            {
                var mockService = new Mock<IBlockchainService>();
                mockService.Setup(m => m.GetChainAsync()).Returns(
                    Task.FromResult(new Chain
                    {
                        BestChainHash = Hash.FromString("BestChainHash"),
                        BestChainHeight = 10L
                    }));

                return mockService.Object;
            });

            services.AddTransient<ISmartContractAddressService>(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(m => m.GetAddressByContractName(It.IsAny<Hash>()))
                    .Returns(SampleAddress.AddressList[0]);

                return mockService.Object;
            });

            //mock consensus service transaction execution result
            services.AddTransient<ITransactionReadOnlyExecutionService>(provider =>
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
                            NextBlockMiningLeftMilliseconds = 4000,
                            ExpectedMiningTime = TimestampHelper.GetUtcNow(),
                            Hint = new AElfConsensusHint {Behaviour = AElfConsensusBehaviour.Nothing}.ToByteString(),
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
                            It.Is<Transaction>(tx => tx.MethodName == "GetInformationToUpdateConsensus"),
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
        }
    }
}