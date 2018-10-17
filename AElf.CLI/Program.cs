using System;
using System.Linq.Expressions;
using AElf.CLI.Certificate;
using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.CLI.Wallet;
using AElf.Common.Application;
using AElf.Cryptography;
using CommandLine;

namespace AElf.CLI
{
    class CommandLineOptions
    {
        [Value(0,
            MetaName = "AElf server address",
            HelpText = "The address of AElf server.",
            Default = "http://localhost:1234")]
        public string ServerAddr { get; set; }
        
        [Value(1,
            MetaName = "AElf data directory",
            HelpText = "The directory the node uses to store data.",
            Default = "")]
        public string DataDir { get; set; }
    }

    class Program
    {
        public static void Main(string[] args)
        {            
            CommandParser parser = new CommandParser();
            var cmdOptions = new CommandLineOptions();
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithNotParsed(err =>
                {
                    Environment.Exit(1);
                }
            ).WithParsed(
                result => { cmdOptions = result; });
            
            ApplicationHelpers.SetDataDir(cmdOptions.DataDir);
            
            ScreenManager screenManager = new ScreenManager();

            AElfKeyStore kstore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
            AccountManager accountManager = new AccountManager(kstore, screenManager);
            CertificatManager certificatManager = new CertificatManager(screenManager);

            AElfCliProgram program = new AElfCliProgram(screenManager, parser, accountManager, certificatManager,
                cmdOptions.ServerAddr);

            // Register local commands
            RegisterAccountCommands(program);
            RegisterNetworkCommands(program);

            program.RegisterCommand(new GetIncrementCmd());
            program.RegisterCommand(new SendTransactionCmd());
            program.RegisterCommand(new LoadContractAbiCmd());
            program.RegisterCommand(new DeployContractCommand());
            program.RegisterCommand(new GetTxResultCmd());
            program.RegisterCommand(new GetGenesisContractAddressCmd());
            program.RegisterCommand(new GetDeserializedResultCmd());
            program.RegisterCommand(new GetBlockHeightCmd());
            program.RegisterCommand(new GetBlockInfoCmd());
            program.RegisterCommand(new CallReadOnlyCmd());
            program.RegisterCommand(new GetMerklePathCmd());
            program.RegisterCommand(new CertificateCmd());

            // Start the CLI
            program.StartRepl();
        }

        private static void RegisterNetworkCommands(AElfCliProgram program)
        {
            program.RegisterCommand(new GetPeersCmd());
            program.RegisterCommand(new AddPeerCommand());
            program.RegisterCommand(new RemovePeerCommand());
            program.RegisterCommand(new GetCommandsCmd());
        }

        private static void RegisterAccountCommands(AElfCliProgram program)
        {
            program.RegisterCommand(new AccountCmd());
        }
    }
}