using System;
using AElf.CLI2.JS;
using Autofac;
using System.IO;
using Alba.CsConsoleFormat.Fluent;
using System.Reflection;
using AElf.CLI2.Utils;
using AElf.Common;
using Org.BouncyCastle.Bcpg;

namespace AElf.CLI2.Commands
{
    public abstract class Command : IDisposable
    {
        protected BaseOption _baseOption;
        protected ILifetimeScope _scope;
        protected IJSEngine _engine;

        public Command(BaseOption option)
        {
            _baseOption = option;
            _scope = IoCContainerBuilder.Build(option);
            _engine = _scope.Resolve<IJSEngine>();
        }

        public void InitChain()
        {
            var accountFile = _baseOption.GetPathForAccount(_baseOption.Account);
            if (!File.Exists(accountFile))
            {
                Colors.WriteLine($@"Account file ""{accountFile}"" doesn't exist.".DarkRed());
                return;
            }

            Console.WriteLine("Unlocking account ...");
            if (string.IsNullOrEmpty(_baseOption.Password))
            {
                _baseOption.Password = ReadLine.ReadPassword("Enter the password: ");
            }

            var acc = new Account()
            {
                PrivateKey = Pem.ReadPrivateKey(accountFile, _baseOption.Password)
            };

            if (!string.IsNullOrEmpty(acc.Mnemonic))
            {
                _engine.RunScript($@"_account = Aelf.wallet.getWalletByMnemonic(""{acc.Mnemonic}"")");
            }

            if (!string.IsNullOrEmpty(acc.PrivateKey))
            {
                _engine.RunScript($@"_account = Aelf.wallet.getWalletByPrivateKey(""{acc.PrivateKey}"")");
            }

            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.init-chain.js"));
        }
        
        public static string GetCode(string path)
        {
            using (var br = File.OpenRead(path))
            {
                return ReadFully(br).ToHex().ToLower();
            }
        }
        
        private static byte[] ReadFully(Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                ms.Position = 0;
                return ms.ToArray();
            }
        }

        public abstract void Execute();

        public virtual void Dispose()
        {
            _scope.Dispose();
        }
    }
}