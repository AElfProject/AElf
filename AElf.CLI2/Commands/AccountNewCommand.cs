using System;
using AElf.CLI2.JS;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI2.Commands
{
    [Verb("account new", HelpText = "Account related options")]
    public class AccountNewOption : BaseOption
    {
        [Value(0, MetaName = "Password", HelpText = "The password of account", Required = true)]
        public string Password { get; set; }
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
            try
            {
                Console.WriteLine(_engine.Get("aelf").Get("wallet").Invoke<JavaScriptValue>("createNewWallet")
                    .ToString());
            }
            catch (JavaScriptUsageException ex)
            {
                throw;
            }
        }
    }
}