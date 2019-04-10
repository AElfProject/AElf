using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.OS.Network;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Tester
{
    public class Tester : ITransientDependency
    {
        private IAbpApplicationWithInternalServiceProvider Application { get; set; }
        
        public ECKeyPair KeyPair { get; }

        public Chain Chain
        {
            get
            {
                var chainManager = Application.ServiceProvider.GetRequiredService<IChainManager>();
                return AsyncHelper.RunSync(() => chainManager.GetAsync());
            }
        }
        
        public Tester(ECKeyPair keyPair, int port, params int[] bootNodes)
        {
            KeyPair = keyPair ?? CryptoHelpers.GenerateKeyPair();
            
            Application =
                AbpApplicationFactory.Create<TesterAElfModule>(options =>
                {
                    options.UseAutofac();

                    options.Services.AddTransient(o =>
                    {
                        var mockService = new Mock<IAccountService>();
                        mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                            Task.FromResult(CryptoHelpers.SignWithPrivateKey(KeyPair.PrivateKey, data)));

                        mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(),
                            It.IsAny<byte[]>()
                        )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                        {
                            var recoverResult =
                                CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                            return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                        });

                        mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(KeyPair.PublicKey);

                        return mockService.Object;
                    });


                    options.Services.Configure<NetworkOptions>(o =>
                    {
                        o.ListeningPort = port;
                        o.BootNodes = bootNodes.ToList().Select(n => $"127.0.0.1:{n.ToString()}").ToList();
                    });
                });
        }

        public async Task StartAsync()
        {
            var chainOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto()
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };

            dto.InitializationSmartContracts.AddGenesisSmartContract<ConsensusContract>(ConsensusSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(TokenSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<DividendContract>(DividendsSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<ResourceContract>(ResourceSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<FeeReceiverContract>(ResourceFeeReceiverSmartContractAddressNameProvider.Name);

            var osService = Application.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            await osService.StartAsync(dto);
        }
    }
}