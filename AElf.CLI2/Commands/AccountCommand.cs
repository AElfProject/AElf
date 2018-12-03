using System;
using System.IO;
using System.Xml.Serialization;
using AElf.CLI2.JS;
using Autofac;
using CommandLine;

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
        public string Mnemonic;
        public string PrivKey;
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

        public void Execute()
        {
            if (_option.Action == AccountAction.create)
            {
                var obj = _engine.Evaluate("Aelf.wallet.createNewWallet()");
                string mnemonic = obj.ReadProperty<string>("mnemonic");
                string privKey = obj.ReadProperty<string>("privateKey");
                string address = obj.ReadProperty<string>("address");
                PrintAccount(address, mnemonic, privKey);
//                var privateKey = Utils.Cryptography.Encrypt(privKey, _option.Password);
//                var encryptedMnemonic = Utils.Cryptography.Encrypt(mnemonic, _option.Password);
//
//                using (var fs = File.Create(_option.AccountFileName))
//                {
//                    var serializer = new XmlSerializer(typeof(Account));
//                    serializer.Serialize(fs, new Account()
//                    {
//                        Mnemonic = encryptedMnemonic,
//                        PrivKey = privateKey,
//                        Address = address
//                    });
//                }
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
                    $@"Your wallet address is {address}",
                    $@"Mnemonic: {mnemonic}",
                    $@"Private Key: {privKey}"
                )
            );
//            if (_option.Action == AccountAction.create)
//            {
//                Console.WriteLine($"The wallet saved to {_option.AccountFileName} with password {_option.Password}.");
//            }
        }
    }
}