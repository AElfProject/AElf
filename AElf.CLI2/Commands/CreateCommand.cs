using System;
using System.IO;
using System.Xml.Serialization;
using AElf.CLI2.JS;
using Autofac;
using CommandLine;
using NLog.Targets;

namespace AElf.CLI2.Commands
{
    [Verb("create", HelpText = "Account related options")]
    public class CreateOption : BaseOption
    {
    }


    public struct Account
    {
        public string EncryptedMnemonic;
        public string EncryptedPrivateKey;
        public string Address;
    }

    public class CreateCommand : ICommand
    {
        private readonly IJSEngine _engine;
        private readonly CreateOption _option;

        public CreateCommand(BaseOption option)
        {
            _option = (CreateOption) option;
            _engine = IoCContainerBuilder.Build(_option).Resolve<IJSEngine>();
        }

        private string PromptPassword()
        {
            const string prompt0 = "Enter a password: ";
            const string prompt1 = "Confirm password: ";
            var p0 = ReadLine.ReadPassword(prompt0);
            var p1 = ReadLine.ReadPassword(prompt1);
            while (p1 != p0)
            {
                Console.WriteLine("Passwords don't match!");
                p1 = ReadLine.ReadPassword(prompt1);
            }

            return p0;
        }

        public void Execute()
        {
            var obj = _engine.Evaluate("Aelf.wallet.createNewWallet()");
            string mnemonic = obj.ReadProperty<string>("mnemonic");
            string privKey = obj.ReadProperty<string>("privateKey");
            string address = obj.ReadProperty<string>("address");
            PrintAccount(address, mnemonic, privKey);

            if (!ReadLine.Read("Saving account info to file? (Y/N): ").Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var password = PromptPassword();
            var encryptedMnemonic = Utils.Cryptography.Encrypt(mnemonic, password);

            var accountFile = _option.GetPathForAccount(address);

            if (File.Exists(accountFile))
            {
                Console.WriteLine($@"Account file ""{accountFile}"" already exists.");
                return;
            }

            using (var fs = File.Create(accountFile))
            {
                var serializer = new XmlSerializer(typeof(Account));
                serializer.Serialize(fs, new Account()
                {
                    EncryptedMnemonic = encryptedMnemonic
                });
            }

            Console.WriteLine($@"Account info has been saved to ""{accountFile}""");
        }

        private void PrintAccount(string address, string mnemonic, string privKey)
        {
            Console.WriteLine(
                string.Join(
                    Environment.NewLine,
                    $@"Your wallet info is :",
                    $@"Mnemonic    : {mnemonic}",
                    $@"Private Key : {privKey}",
                    $@"Address     : {address}"
                )
            );
        }
    }
}