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
    [Verb("deploy", HelpText = "Deploy a smart contract.")]
    public class DeployOption : BaseOption
    {
        [Value(0, MetaName = "category", HelpText = "The category of the contract to be deployed.", Required = true)]
        public int Category { get; set; }

        [Value(1, MetaName = "codefile", HelpText = "The compiled contract code file of the contract to be deployed.",
            Required = true)]
        public string Codefile { get; set; }
    }

    public class DeployCommand : Command
    {
        private readonly DeployOption _option;

        public DeployCommand(DeployOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            if (!File.Exists(_option.Codefile))
            {
                Colors.WriteLine($@"Code file ""{_option.Codefile}"" doesn't exist.".DarkRed());
            }
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.deploy-command.js"));
            _engine.GlobalObject.CallMethod<int, string>("deployCommand", _option.Category,
                GetCode(_option.Codefile));
        }
    }
}