using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Modularity;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;
using AElf.CSharp.Core.Extension;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    [DependsOn(
        typeof(KernelTestAElfModule),
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
                        LastIrreversibleBlockHash = Hash.FromString("LastIrreversibleBlockHash")
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

            context.Services.AddTransient(provider =>
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
                var consensusHash = ConsensusSmartContractAddressNameProvider.Name;
                mockService.Setup(o => o.GetAddressByContractName(It.Is<Hash>(hash => hash != consensusHash)))
                    .Returns(SampleAddress.AddressList[0]);
                mockService.Setup(o =>
                        o.GetAddressByContractName(It.Is<Hash>(hash => hash == consensusHash)))
                    .Returns(SampleAddress.AddressList[1]);

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

                return mockService.Object;
            });

            context.Services.AddTransient(provider =>
            {
                var encryptDic = new Dictionary<string, byte[]>
                {
                    {"bp1", Hash.FromString("encrypt info").Value.ToByteArray()}
                };

                var decryptDic = new Dictionary<string, byte[]>
                {
                    {"bp2", Hash.FromString("decrypt info").Value.ToByteArray()}
                };

                var inValuesDic = new Dictionary<string, Hash> {{"bp3", Hash.FromString("in values")}};

                var mockService = new Mock<ISecretSharingService>();
                mockService.Setup(m => m.GetEncryptedPieces(It.IsAny<long>()))
                    .Returns(encryptDic);
                mockService.Setup(m => m.GetDecryptedPieces(It.IsAny<long>()))
                    .Returns(decryptDic);
                mockService.Setup(m => m.GetRevealedInValues(It.IsAny<long>()))
                    .Returns(inValuesDic);

                return mockService.Object;
            });
        }
    }
}