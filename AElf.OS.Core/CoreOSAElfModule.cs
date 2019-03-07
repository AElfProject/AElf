using System;
using System.Runtime.InteropServices;
using System.Security;
using AElf.Common;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using AElf.OS.Account;
using AElf.OS.Handlers;
using AElf.OS.Jobs;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(CoreKernelAElfModule)), DependsOn(typeof(AbpBackgroundJobsModule))]
    public class CoreOSAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            context.Services.AddAssemblyOf<CoreOSAElfModule>();


            //Configure<ChainOptions>(option => option.ChainId = ChainHelpers.ConvertBase58ToChainId(configuration["ChainId"]));

            Configure<AccountOptions>(configuration.GetSection("Account"));
            Configure<NetworkOptions>(configuration.GetSection("Network"));
            Configure<DPoSOptions>(configuration.GetSection("Consensus"));

            context.Services.AddSingleton<PeerConnectedEventHandler>();
            context.Services.AddTransient<ForkDownloadJob>();

            var keyStore = new AElfKeyStore(ApplicationHelper.AppDataPath);
            context.Services.AddSingleton<IKeyStore>(keyStore);
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var accountOptions = context.ServiceProvider.GetService<IOptions<AccountOptions>>().Value;

            /*var keyStore = context.ServiceProvider.GetService<IKeyStore>();

            if (string.IsNullOrWhiteSpace(accountOptions.NodeAccount))
            {
                throw new Exception("NodeAccount is needed");
            }

            try
            {
                var password = string.IsNullOrWhiteSpace(accountOptions.NodeAccountPassword)
                    ? AskInvisible()
                    : accountOptions.NodeAccountPassword;
                keyStore.OpenAsync(accountOptions.NodeAccount, password, false).Wait();
                var nodeKey = keyStore.GetAccountKeyPair(accountOptions.NodeAccount);
                if (nodeKey == null)
                {
                    Console.WriteLine("Load keystore failed.");
                    Environment.Exit(-1);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Load keystore failed.", e);
            }*/
        }

        /*
        private static string AskInvisible()
        {
            Console.Write("Node account password: ");
            var securePassword = new SecureString();
            while (true)
            {
                var consoleKeyInfo = Console.ReadKey(true);
                if (consoleKeyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (consoleKeyInfo.Key == ConsoleKey.Backspace)
                {
                    if (securePassword.Length > 0)
                    {
                        securePassword.RemoveAt(securePassword.Length - 1);
                    }
                }
                else
                {
                    securePassword.AppendChar(consoleKeyInfo.KeyChar);
                }
            }

            Console.WriteLine();

            var intPtr = IntPtr.Zero;
            try
            {
                intPtr = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(intPtr);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception while get account password.", ex);
            }
            finally
            {
                if (intPtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(intPtr);
            }
        }*/
    }
}