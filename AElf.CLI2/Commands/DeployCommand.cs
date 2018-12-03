using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using AElf.CLI2.JS;
using AElf.Common;
using Alba.CsConsoleFormat.Fluent;
using Autofac;
using ChakraCore.NET;
using CommandLine;

namespace AElf.CLI2.Commands
{
    [Verb("deploy", HelpText = "Account related options")]
    public class DeployOption : BaseOption
    {
        [Value(0, MetaName = "category", HelpText = "The category of the contract to be deployed.", Required = true)]
        public int Category { get; set; }

        [Value(1, MetaName = "codefile", HelpText = "The compiled contract code file of the contract to be deployed.",
            Required = true)]
        public string Codefile { get; set; }
    }

    public class DeployCommand : ICommand
    {
        private readonly IJSEngine _engine;
        private DeployOption _option;

        public DeployCommand(DeployOption option)
        {
            _option = option;
            _engine = IoCContainerBuilder.Build(_option).Resolve<IJSEngine>();
        }

        public void Execute()
        {
            var accountfile = _option.GetPathForAccount(_option.Account);
            if (!File.Exists(accountfile))
            {
                Colors.WriteLine($@"Account file ""{accountfile}"" doesn't exist.".DarkRed());
            }

            if (!File.Exists(_option.Codefile))
            {
                Colors.WriteLine($@"Code file ""{_option.Codefile}"" doesn't exist.".DarkRed());
            }

            Console.WriteLine("Unlocking account ...");
            var p = ReadLine.ReadPassword("Enter the password: ");


            var acc = EncryptedAccount.LoadFromFile(accountfile).Decrypt(p);

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
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.deploy-command.js"));
            _engine.GlobalObject.CallFunctionAsync<int, string, int>("deployCommand", _option.Category,
                GetCode(_option.Codefile)).Wait();
        }

        private static string GetCode(string path)
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
    }
}