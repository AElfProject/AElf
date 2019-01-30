using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account;
using AElf.Modularity;
using AElf.OS.Account;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.Tests
{
    [DependsOn(typeof(TestBaseAElfModule))]
    public class TestsOSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var ecKeyPair = new KeyPairGenerator().Generate();
            var nodeAccount = Address.FromPublicKey(ecKeyPair.PublicKey).GetFormatted();
            var nodeAccountPassword = "123";
            
            Configure<AccountOptions>(o =>
            {
                o.NodeAccount = nodeAccount;
                o.NodeAccountPassword = nodeAccountPassword;
            });
            
            context.Services.AddSingleton<IKeyStore>(o =>
                Mock.Of<IKeyStore>(
                    c => c.OpenAsync(nodeAccount, nodeAccountPassword,false) ==
                         Task.FromResult(AElfKeyStore.Errors.None) &&
                         c.GetAccountKeyPair(nodeAccount) == ecKeyPair)
            );

            context.Services.AddTransient<IAccountService, AccountService>();
        }
    }
}