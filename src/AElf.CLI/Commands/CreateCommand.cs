using System;
using System.IO;
using System.Xml.Serialization;
using AElf.CLI.JS;
using AElf.CLI.Utils;
using ChakraCore.NET;
using CommandLine;

namespace AElf.CLI.Commands
{
    [Verb("create", HelpText = "Create a new account.")]
    public class CreateOption : BaseOption
    {
    }


    public class CreateCommand : Command
    {
        private readonly CreateOption _option;

        public CreateCommand(BaseOption option) : base(option)
        {
            _option = (CreateOption) option;
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

        public override void Execute()
        {
            var obj = _engine.Evaluate("Aelf.wallet.createNewWallet()");
            string mnemonic = obj.ReadProperty<string>("mnemonic");
            string privKey = obj.ReadProperty<string>("privateKey");
            string address = obj.ReadProperty<string>("address");
            JSValue keyPair = obj.ReadProperty<JSValue>("keyPair");
            string pubKey = keyPair.CallFunction<string, string>("getPublic", "hex");
            PrintAccount(address, mnemonic, privKey, pubKey);

            if (!ReadLine.Read("Saving account info to file? (Y/N): ").Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var password = PromptPassword();

            var accountFile = _option.GetPathForAccount(address);

            Pem.WriteKeyPair(accountFile, privKey, pubKey, password);
        }

        private void PrintAccount(string address, string mnemonic, string privKey, string pubKey)
        {
            Console.WriteLine(
                string.Join(
                    Environment.NewLine,
                    @"Your wallet info is :",
                    $@"Mnemonic    : {mnemonic}",
                    $@"Private Key : {privKey}",
                    $@"Public Key : {pubKey}",
                    $@"Address     : {address}"
                )
            );
        }
    }
}