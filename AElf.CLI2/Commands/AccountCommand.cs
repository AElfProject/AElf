using System;
using System.IO;
using System.Xml.Serialization;
using AElf.CLI2.JS;
using Autofac;
using CommandLine;
using NLog.Targets;

namespace AElf.CLI2.Commands
{
    public enum AccountAction
    {
        create,
        print
    }

    [Verb("account", HelpText = "Account related options")]
    public class AccountOption : BaseOption
    {
        [Value(0, MetaName = "action", HelpText = "Values: [create/print]", Required = true)]
        public AccountAction Action { get; set; }

//        [Value(1, MetaName = "filename", HelpText = "The account file name.", Required = true)]
//        public string AccountFileName { get; set; }
//
//        [Value(2, MetaName = "password", HelpText = "The password of account.", Required = true)]
//        public string Password { get; set; }
    }


    public struct Account
    {
        public string EncryptedMnemonic;
        public string EncryptedPrivateKey;
        public string Address;
    }

    public class AccountCommand : ICommand
    {
        private IJSEngine _engine;
        private AccountOption _option;

        public AccountCommand(BaseOption option)
        {
            _option = (AccountOption) option;
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
            if (_option.Action == AccountAction.create)
            {
                var obj = _engine.Evaluate("Aelf.wallet.createNewWallet()");
                string mnemonic = obj.ReadProperty<string>("mnemonic");
                string privKey = obj.ReadProperty<string>("privateKey");
                string address = obj.ReadProperty<string>("address");
                PrintAccount(address, mnemonic, privKey);

                Console.WriteLine("Saving account info to file.");
                var password = PromptPassword();
                var encryptedMnemonic = Utils.Cryptography.Encrypt(mnemonic, password);

                var walletAccountFile = $"{address}.xml";
                using (var fs = File.Create(walletAccountFile))
                {
                    var serializer = new XmlSerializer(typeof(Account));
                    serializer.Serialize(fs, new Account()
                    {
                        EncryptedMnemonic = encryptedMnemonic
                    });
                }

                Console.WriteLine($"Account info has been saved to {Path.GetFullPath(walletAccountFile)}");
            }
            else
            {
//                Account acc = new Account();
//                using (var fs = File.OpenRead(_option.AccountFileName))
//                {
//                    var serializer = new XmlSerializer(typeof(Account));
//                    acc = (Account) serializer.Deserialize(fs);
//                }
//
//                acc.Mnemonic = Utils.Cryptography.Decrypt(acc.Mnemonic, _option.Password);
//                acc.PrivKey = Utils.Cryptography.Decrypt(acc.PrivKey, _option.Password);
//                PrintAccount(acc.Address, acc.Mnemonic, acc.PrivKey);
            }
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