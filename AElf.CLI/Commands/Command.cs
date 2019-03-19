using System;
using AElf.CLI.JS;
using System.IO;
using Alba.CsConsoleFormat.Fluent;
using AElf.CLI.Utils;
using AElf.Common;
using Autofac;

namespace AElf.CLI.Commands
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

            Console.WriteLine("Your public key is ");
            _engine.RunScript(@"console.log(_account.keyPair.pub.encode('hex'))");

            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "init-chain.js")));
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
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
                return memoryStream.ToArray();
            }
        }

        public abstract void Execute();

        public virtual void Dispose()
        {
            _scope.Dispose();
        }
    }
}