using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    [DependsOn(
        typeof(CoreAElfModule),
        typeof(KernelAElfModule),
        typeof(AEDPoSAElfModule))]
    public class AEDPoSTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient(o =>
            {
                var mockService = new Mock<IAccountService>();
                return mockService.Object;
            });

            var _interestedEvent = new IrreversibleBlockFound();
            var _logEvent = _interestedEvent.ToLogEvent(SampleAddress.AddressList[0]);
            
            context.Services.AddTransient(provider =>
            {
                var mockBlockChainService = new Mock<IBlockchainService>();
                mockBlockChainService.Setup(m => m.GetChainAsync()).Returns(() =>
                {
                    var chain = new Chain
                    {
                        LastIrreversibleBlockHeight = 10,
                        LastIrreversibleBlockHash = Hash.FromString("LastIrreversibleBlockHash")
                    };

                    return Task.FromResult(chain);
                });

                mockBlockChainService.Setup(m => m.GetBlockByHashAsync(It.IsAny<Hash>())).Returns(Task.FromResult(
                    new Block
                    {
                        Header = new BlockHeader
                        {
                            Bloom = ByteString.CopyFrom(_logEvent.GetBloom().Data),
                            Height = 15
                        },
                        Body = new BlockBody
                        {
                            TransactionIds =
                            {
                                Hash.FromString("not exist"),
                                Hash.FromString("failed case"),
                                Hash.FromString("mined case")
                            }
                        }
                    }
                ));

                mockBlockChainService.Setup(m =>
                        m.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(Task.FromResult(new Hash()));
                return mockBlockChainService.Object;
            });

            context.Services.AddTransient<ITransactionResultQueryService>(provider =>
            {
                var mockService = new Mock<ITransactionResultQueryService>();
                mockService.Setup(m => m.GetTransactionResultAsync(It.IsIn(Hash.FromString("not exist"))))
                    .Returns(Task.FromResult<TransactionResult>(null));
                mockService.Setup(m => m.GetTransactionResultAsync(It.IsIn(Hash.FromString("failed case"))))
                    .Returns(Task.FromResult(new TransactionResult
                    {
                        Error = "failed due to some reason",
                        Status = TransactionResultStatus.Failed
                    }));
                mockService.Setup(m => m.GetTransactionResultAsync(It.IsIn(Hash.FromString("mined case"))))
                    .Returns(Task.FromResult(new TransactionResult
                    {
                        Status = TransactionResultStatus.Mined,
                        Bloom = ByteString.CopyFrom(_logEvent.GetBloom().Data),
                        Logs = { new LogEvent
                        {
                            Address = SampleAddress.AddressList[0],
                            Name = _logEvent.Name,
                        }}
                    }));
                
                return mockService.Object;
            });

            context.Services.AddTransient<ISmartContractAddressService>(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(o => o.GetAddressByContractName(It.IsAny<Hash>()))
                    .Returns(SampleAddress.AddressList[0]);

                return mockService.Object;
            });

            context.Services.AddTransient<IConsensusService>(provider =>
            {
                var mockService = new Mock<IConsensusService>();
                mockService.Setup(m => m.GetInformationToUpdateConsensusAsync(It.IsAny<ChainContext>())).Returns(
                    Task.FromResult(ByteString.CopyFromUtf8("test").ToByteArray()));

                mockService.Setup(m => m.TriggerConsensusAsync(It.IsAny<ChainContext>())).Returns(Task.CompletedTask);

                return mockService.Object;
            });

            context.Services.AddTransient<ITransactionReadOnlyExecutionService>(provider =>
            {
                var mockService = new Mock<ITransactionReadOnlyExecutionService>();
                mockService.Setup(m =>
                        m.ExecuteAsync(It.IsAny<ChainContext>(),
                            It.Is<Transaction>(tx =>
                                tx.MethodName == "GetCurrentMinerList"),
                            It.IsAny<Timestamp>()))
                    .Returns(Task.FromResult(new TransactionTrace
                    {
                        ExecutionStatus = ExecutionStatus.Executed,
                        ReturnValue = ByteString.CopyFrom(new MinerList
                        {
                            Pubkeys =
                            {
                                ByteString.CopyFromUtf8("bp1"),
                                ByteString.CopyFromUtf8("bp2"),
                                ByteString.CopyFromUtf8("bp3")
                            }
                        }.ToByteArray())
                    }));
                
                return mockService.Object;
            });
        }
    }
}