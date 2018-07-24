using System;
using System.IO;
using System.Xml.Serialization;
using AElf.CLI2.JS;
using Akka.Routing;
using CommandLine;

namespace AElf.CLI2.Commands
{
    [Verb("create_account", HelpText = "Account related options")]
    public class AccountNewOption : BaseOption
    {
        [Value(0, MetaName = "account file name", HelpText = "The account file name", Required = true)]
        public string AccountFileName { get; set; }
        [Value(1, MetaName = "Password", HelpText = "The password of account", Required = true)]
        public string Password { get; set; }
    }


    public struct Account
    {
        public string Mnemonic;
        public string PrivKey;
        public string Address;
    }

    public class AccountNewCommand : ICommand
    {
        private IJSEngine _engine;
        private AccountNewOption _option;

        public AccountNewCommand(IJSEngine engine, BaseOption option)
        {
            _engine = engine;
            _option = (AccountNewOption) option;
        }

        public void Execute()
        {
            var result = _engine.Get("aelf").Get("wallet").InvokeAndGetJSObject("createNewWallet");
            string mnemonic = result.Get("mnemonic").Value.ToString();
            string privKey = result.Get("privKey").Value.ToString();
            string address = result.Get("address").Value.ToString();
            Console.WriteLine($@"Your wallet address is {address}
Mnemonic: {mnemonic}
Private Key: {privKey}
The wallet saved to {_option.AccountFileName} with password {_option.Password}.
");
            var privateKey = Utils.Cryptography.Encrypt(privKey, _option.Password);
            var encryptedMnemonic = Utils.Cryptography.Encrypt(mnemonic, _option.Password);

            using (var fs = File.OpenWrite(_option.AccountFileName))
            {
                var serializer = new XmlSerializer(typeof(Account));
                serializer.Serialize(fs, new Account()
                {
                    Mnemonic = encryptedMnemonic,
                    PrivKey = privateKey,
                    Address = address
                });
            }
        }
    }
}