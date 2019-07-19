using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    [DependsOn(
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
            var _logEvent = _interestedEvent.ToLogEvent(Address.FromString("test"));
            
            context.Services.AddTransient(provider =>
            {
                var mockBlockChainService = new Mock<IBlockchainService>();
                mockBlockChainService.Setup(m => m.GetChainAsync()).Returns(() =>
                {
                    var chain = new Chain();
                    chain.LastIrreversibleBlockHeight = 10;
                    chain.LastIrreversibleBlockHash = Hash.Generate();

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
                            Transactions =
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
                            Address = Address.FromString("test"),
                            Name = _logEvent.Name,
                        }}
                    }));
                
                return mockService.Object;
            });

            context.Services.AddTransient<ISmartContractAddressService>(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(o => o.GetAddressByContractName(It.IsAny<Hash>()))
                    .Returns(Address.FromString("test"));

                return mockService.Object;
            });
        }
    }
}