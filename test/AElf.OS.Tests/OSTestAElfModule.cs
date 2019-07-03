using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Modularity;
using AElf.OS.Account.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(OSAElfModule),
        typeof(OSCoreWithChainTestAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class OSTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = ChainHelper.ConvertBase58ToChainId("AELF"); });

            var ecKeyPair = CryptoHelper.GenerateKeyPair();
            var nodeAccount = Address.FromPublicKey(ecKeyPair.PublicKey).GetFormatted();
            var nodeAccountPassword = "123";

            Configure<AccountOptions>(o =>
            {
                o.NodeAccount = nodeAccount;
                o.NodeAccountPassword = nodeAccountPassword;
            });

            context.Services.AddSingleton<IKeyStore>(o =>
            {
                var keyStore = new Mock<IKeyStore>();
                ECKeyPair keyPair = null;

                keyStore.Setup(k => k.GetAccountKeyPair(It.IsAny<string>())).Returns(() => keyPair);

                keyStore.Setup(k => k.UnlockAccountAsync(It.IsAny<string>(), It.IsAny<string>(), false)).Returns(() =>
                {
                    keyPair = ecKeyPair;
                    return Task.FromResult(AElfKeyStore.Errors.None);
                });

                return keyStore.Object;
            });

            context.Services.AddTransient<AccountService>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }
    }
}