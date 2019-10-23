using System.Collections.Generic;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Modularity;
using AElf.OS.Network;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS
{
    [DependsOn(typeof(OSCoreWithChainTestAElfModule))]
    public class OSCoreHandshakeTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var authorizedKeyPair = CryptoHelper.GenerateKeyPair();

            Configure<NetworkOptions>(o =>
            {
                // for convenience when testing self connection the local key-pair will be part of the authorized.
                var accountService = context.Services.GetRequiredServiceLazy<IAccountService>().Value;
                var pubkey = AsyncHelper.RunSync(async () => (await accountService.GetPublicKeyAsync()).ToHex());
                
                o.AuthorizedPeers = AuthorizedPeers.Authorized;
                o.AuthorizedKeys = new List<string> {authorizedKeyPair.PublicKey.ToHex(), pubkey};
            });
            
            context.Services.AddSingleton(new TestPeerKeyProvider { AuthorizedKey = authorizedKeyPair });
        }
    }
}