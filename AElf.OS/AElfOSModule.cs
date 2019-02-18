using System;
using System.Runtime.InteropServices;
using System.Security;
using AElf.Common.Application;
using AElf.Configuration;
using AElf.Consensus;
using AElf.Consensus.DPoS;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Modularity;
using AElf.OS.Account;
using AElf.OS.Network;
using AElf.OS.Network.Temp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(KernelAElfModule))]
    public class OSAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<AccountOptions>(configuration.GetSection("Account"));
            Configure<NetworkOptions>(configuration.GetSection("Network"));
            Configure<DPoSOptions>(configuration.GetSection("Consensus"));
            
            var keyStore = new AElfKeyStore(ApplicationHelpers.ConfigPath);
            context.Services.AddSingleton<IKeyStore>(keyStore);
            context.Services.AddTransient<IAccountService, AccountService>();
            
            //todo temp remove after peer service is wired up with the correct service 
             context.Services.AddSingleton<IBlockService, BlockService>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var accountOptions = context.ServiceProvider.GetService<IOptions<AccountOptions>>().Value;
            var keyStore = context.ServiceProvider.GetService<IKeyStore>();
            
            if (string.IsNullOrWhiteSpace(accountOptions.NodeAccount))
            {
                throw new Exception("NodeAccount is needed");
            }
            try
            {
                var password = string.IsNullOrWhiteSpace(accountOptions.NodeAccountPassword) ? AskInvisible() : accountOptions.NodeAccountPassword;
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
            }
        }
        
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
        }
    }
}