using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using AElf.CLI.Certificate;
using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Command.Election;
using AElf.CLI.Command.MultiSig;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.CLI.Wallet;
using AElf.Common.Application;
using AElf.Cryptography;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public string ConfigPath { get; set; }
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
            
            ApplicationHelpers.SetConfigPath(cmdOptions.ConfigPath);
            
            ScreenManager screenManager = new ScreenManager();

            AElfKeyStore kstore = new AElfKeyStore(ApplicationHelpers.GetDefaultConfigPath());
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
            program.RegisterCommand(new UpdateContractCommand());
            program.RegisterCommand(new GetTxResultCmd());
            program.RegisterCommand(new GetGenesisContractAddressCmd());
            program.RegisterCommand(new GetDeserializedResultCmd());
            program.RegisterCommand(new GetBlockHeightCmd());
            program.RegisterCommand(new GetBlockInfoCmd());
            program.RegisterCommand(new CallReadOnlyCmd());
            
            program.RegisterCommand(new GetMerklePathCmd());
            program.RegisterCommand(new CertificateCmd());
            
            program.RegisterCommand(new CreateMSigCmd());
            program.RegisterCommand(new ProposeCmd());
            program.RegisterCommand(new CheckProposalCmd());
            program.RegisterCommand(new ApproveCmd());
            program.RegisterCommand(new ReleaseProposalCmd());
            
            program.RegisterCommand(new AnnounceElectionCmd());

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