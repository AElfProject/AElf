using System;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;
using AElf.Contracts.Consensus.DPoS;

namespace AElf.Kernel.Consensus
{
    [DependsOn(typeof(TestBaseKernelAElfModule))]
    public class ConsensusTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            var ecKeyPair = CryptoHelpers.GenerateKeyPair();

            var dposTriggerInformation = new DPoSTriggerInformation()
            {
                PublicKey = ByteString.CopyFrom(ecKeyPair.PublicKey),
                InitialTermNumber = 1,
                Behaviour = DPoSBehaviour.NextTerm
            };
            services.AddTransient(o =>
            {
                var mockService = new Mock<IConsensusInformationGenerationService>();
                mockService.Setup(m => m.GetTriggerInformation(TriggerType.ConsensusTransactions)).Returns(
                    dposTriggerInformation);
                mockService.Setup(m => m.GetTriggerInformation(TriggerType.BlockHeaderExtraData)).Returns(
                    dposTriggerInformation);
                mockService.Setup(m => m.GetTriggerInformation(TriggerType.ConsensusCommand)).Returns(
                    new CommandInput {PublicKey = ByteString.CopyFrom(ecKeyPair.PublicKey)});
                mockService.Setup(m => m.ParseConsensusTriggerInformation(It.IsAny<byte[]>())).Returns(
                    dposTriggerInformation);
                mockService.Setup(m => m.ExecuteContractAsync<ConsensusCommand>(It.IsAny<IChainContext>(),
                        It.IsAny<string>(), It.IsAny<IMessage>(), It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(new ConsensusCommand
                    {
                        NextBlockMiningLeftMilliseconds = 4000,
                        ExpectedMiningTime = DateTime.UtcNow.Add(TimeSpan.FromSeconds(4)).ToTimestamp()
                    }));
                mockService.Setup(m => m.ExecuteContractAsync<ValidationResult>(It.IsAny<IChainContext>(),
                        It.IsAny<string>(), It.IsAny<IMessage>(), It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(new ValidationResult
                    {
                        Success = true
                    }));
                mockService.Setup(m => m.ExecuteContractAsync<TransactionList>(It.IsAny<IChainContext>(),
                        It.IsAny<string>(), It.IsAny<IMessage>(), It.IsAny<DateTime>()))
                    .Returns(Task.FromResult(new TransactionList
                    {
                        Transactions =
                        {
                            new Transaction{ MethodName = ConsensusConsts.GenerateConsensusTransactions, Params = ByteString.CopyFromUtf8("test1")},
                            new Transaction{ MethodName = ConsensusConsts.GenerateConsensusTransactions, Params = ByteString.CopyFromUtf8("test2")},
                            new Transaction{ MethodName = ConsensusConsts.GenerateConsensusTransactions, Params = ByteString.CopyFromUtf8("test3")}
                        }
                    }));
                return mockService.Object;
            });
            
            services.AddTransient(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelpers.SignWithPrivateKey(ecKeyPair.PrivateKey, data)));

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(ecKeyPair.PublicKey);

                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<IConsensusScheduler>();

                return mockService.Object;
            });
            services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockchainService>();
                mockService.Setup(m=>m.GetChainAsync()).Returns(
                    Task.FromResult(new Chain()
                    {
                        BestChainHash = Hash.Empty,
                        BestChainHeight = 100
                    }));

                return mockService.Object;
            });
            services.AddTransient<ConsensusControlInformation>();
            services.AddTransient(o =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(m=>m.GetAddressByContractName(It.IsAny<Hash>())).Returns(
                    Address.Generate());
                return mockService.Object;
            });
            
            services.AddTransient<IConsensusService, ConsensusService>();
            services.AddTransient<ISystemTransactionGenerator, ConsensusTransactionGenerator>();
        }
    }
}