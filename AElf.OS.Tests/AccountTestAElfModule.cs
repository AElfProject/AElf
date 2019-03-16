using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(OSTestAElfModule)
    )]
    public class AccountTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var ecKeyPair = CryptoHelpers.GenerateKeyPair();
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

                    keyStore.Setup(k => k.OpenAsync(It.IsAny<string>(), It.IsAny<string>(), false)).Returns(() =>
                    {
                        keyPair = ecKeyPair;
                        return Task.FromResult(AElfKeyStore.Errors.None);
                    });

                    return keyStore.Object;
                }
            );
        }
    }
}