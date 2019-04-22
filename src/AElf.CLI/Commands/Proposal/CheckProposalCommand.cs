using System;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI.Commands.Proposal
{
    [Verb("check-proposal", HelpText = "Check proposal status.")]
    public class CheckProposalOption : BaseOption
    {
        [Value(0, HelpText = "Hash of proposal you want to check. ", Required = true)]
        public string ProposalHash { get; set; } = "";
    }
    public class CheckProposalCommand : Command
    {
        private readonly CheckProposalOption _option;

        public CheckProposalCommand(CheckProposalOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            if (string.IsNullOrEmpty(_option.Endpoint))
            {
                Colors.WriteLine("Endpoint is not provided. Cannot proceed.".DarkRed());
                return;
            }


            if (_option.ProposalHash.Length != 64)
            {
                Colors.WriteLine("Invalid proposal hash is provided.".DarkRed());
                return;
            }

            try
            {
                // Get res
                _engine.RunScript($@"
                    var res = aelf.chain.checkProposal(""{_option.ProposalHash}"");
                ");
                // Format res
                _engine.RunScript($@"
                    var resStr = JSON.stringify(res, null, 2);
                ");
                Console.WriteLine(_engine.GlobalObject.ReadProperty<string>("resStr"));
            }
            catch (JavaScriptException e)
            {
                Colors.WriteLine(e.Message.Replace("Script threw an exception. ", "").DarkRed());
            }
        }
    }
}