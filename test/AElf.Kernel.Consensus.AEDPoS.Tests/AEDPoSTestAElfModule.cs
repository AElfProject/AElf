using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS.Tests;

[DependsOn(
    typeof(KernelTestAElfModule),
    typeof(SmartContractAElfModule),
    typeof(AEDPoSAElfModule))]
// ReSharper disable once InconsistentNaming
public class AEDPoSTestAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var interestedEvent = new IrreversibleBlockFound();
        var logEvent = interestedEvent.ToLogEvent(SampleAddress.AddressList[0]);

        context.Services.AddTransient(provider =>
        {
            var mockBlockChainService = new Mock<IBlockchainService>();
            mockBlockChainService.Setup(m => m.GetChainAsync()).Returns(() =>
            {
                var chain = new Chain
                {
                    LastIrreversibleBlockHeight = 10,
                    LastIrreversibleBlockHash = HashHelper.ComputeFrom("LastIrreversibleBlockHash")
                };

                return Task.FromResult(chain);
            });

            mockBlockChainService.Setup(m => m.GetBlockByHashAsync(It.IsAny<Hash>())).Returns(Task.FromResult(
                new Block
                {
                    Header = new BlockHeader
                    {
                        Bloom = ByteString.CopyFrom(logEvent.GetBloom().Data),
                        Height = 15
                    },
                    Body = new BlockBody
                    {
                        TransactionIds =
                        {
                            HashHelper.ComputeFrom("not exist"),
                            HashHelper.ComputeFrom("failed case"),
                            HashHelper.ComputeFrom("mined case")
                        }
                    }
                }
            ));

            mockBlockChainService.Setup(m =>
                    m.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<long>(), It.IsAny<Hash>()))
                .Returns(Task.FromResult(new Hash()));
            return mockBlockChainService.Object;
        });

        context.Services.AddTransient(provider =>
        {
            var mockService = new Mock<ITransactionResultQueryService>();
            mockService.Setup(m => m.GetTransactionResultAsync(It.IsIn(HashHelper.ComputeFrom("not exist"))))
                .Returns(Task.FromResult<TransactionResult>(null));
            mockService.Setup(m => m.GetTransactionResultAsync(It.IsIn(HashHelper.ComputeFrom("failed case"))))
                .Returns(Task.FromResult(new TransactionResult
                {
                    Error = "failed due to some reason",
                    Status = TransactionResultStatus.Failed
                }));
            mockService.Setup(m => m.GetTransactionResultAsync(It.IsIn(HashHelper.ComputeFrom("mined case"))))
                .Returns(Task.FromResult(new TransactionResult
                {
                    Status = TransactionResultStatus.Mined,
                    Bloom = ByteString.CopyFrom(logEvent.GetBloom().Data),
                    Logs =
                    {
                        new LogEvent
                        {
                            Address = SampleAddress.AddressList[0],
                            Name = logEvent.Name
                        }
                    }
                }));

            return mockService.Object;
        });

        context.Services.AddTransient(provider =>
        {
            var mockService = new Mock<ISmartContractAddressService>();
            var consensusHash = ConsensusSmartContractAddressNameProvider.StringName;
            mockService.Setup(o =>
                    o.GetAddressByContractNameAsync(It.IsAny<IChainContext>(),
                        It.Is<string>(hash => hash != consensusHash)))
                .Returns(Task.FromResult(SampleAddress.AddressList[0]));
            mockService.Setup(o =>
                    o.GetAddressByContractNameAsync(It.IsAny<IChainContext>(),
                        It.Is<string>(hash => hash == consensusHash)))
                .Returns(Task.FromResult(SampleAddress.AddressList[1]));

            return mockService.Object;
        });

        context.Services.AddTransient(provider =>
        {
            var mockService = new Mock<IBlockExtraDataService>();
            mockService.Setup(m => m.GetExtraDataFromBlockHeader("Consensus",
                    It.Is<BlockHeader>(o => o != null)))
                .Returns(ByteString.CopyFrom(new AElfConsensusHeaderInformation
                {
                    Behaviour = AElfConsensusBehaviour.UpdateValue,
                    SenderPubkey = ByteString.CopyFromUtf8("real-pubkey"),
                    Round = new Round()
                }.ToByteArray()));
            mockService.Setup(m => m.GetExtraDataFromBlockHeader("Consensus",
                    It.Is<BlockHeader>(o => o == null)))
                .Returns(ByteString.CopyFrom(new AElfConsensusHeaderInformation
                {
                    Behaviour = AElfConsensusBehaviour.Nothing,
                    SenderPubkey = ByteString.CopyFromUtf8("real-pubkey"),
                    Round = new Round()
                }.ToByteArray()));

            return mockService.Object;
        });

        context.Services.AddTransient(provider =>
        {
            var mockService = new Mock<IConsensusService>();
            mockService.Setup(m => m.GetConsensusExtraDataAsync(It.IsAny<ChainContext>())).Returns(
                Task.FromResult(ByteString.CopyFromUtf8("test").ToByteArray()));

            mockService.Setup(m => m.TriggerConsensusAsync(It.IsAny<ChainContext>())).Returns(Task.CompletedTask);

            return mockService.Object;
        });

        context.Services.AddTransient(provider =>
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
            
            mockService.Setup(m =>
                    m.ExecuteAsync(It.IsAny<ChainContext>(),
                        It.Is<Transaction>(tx =>
                            tx.MethodName == "GetRandomHash"),
                        It.IsAny<Timestamp>()))
                .Returns(Task.FromResult(new TransactionTrace
                {
                    ExecutionStatus = ExecutionStatus.Executed,
                    ReturnValue = Hash.Empty.ToByteString()
                }));

            return mockService.Object;
        });

        context.Services.AddSingleton<IrreversibleBlockHeightUnacceptableLogEventProcessor>();
        context.Services.AddSingleton<ITransactionPackingOptionProvider, MockTransactionPackingOptionProvider>();
    }
}