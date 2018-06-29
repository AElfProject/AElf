using System;
using System.Linq.Expressions;
using AElf.CLI.Command;
using AElf.CLI.Command.Account;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.CLI.Wallet;
using AElf.Common.Application;
using AElf.Cryptography;

namespace AElf.CLI
{
    class Program
    {
        // todo Parse command line options
        public static void Main(string[] args)
        {
            ScreenManager screenManager = new ScreenManager();
            CommandParser parser = new CommandParser();
            
            AElfKeyStore kstore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
            AccountManager manager = new AccountManager(kstore, screenManager);
            
            int port = args.Length > 0 ? Int32.Parse(args[0]) : 5000;
            
            AElfCliProgram program = new AElfCliProgram(screenManager, parser, manager, port);

            // Register local commands
            RegisterAccountCommands(program);
            RegisterNetworkCommands(program);
            
            program.RegisterCommand(new GetIncrementCmd());
            program.RegisterCommand(new SendTransactionCmd());
            program.RegisterCommand(new LoadContractAbiCmd());
            program.RegisterCommand(new DeployContractCommand());
            program.RegisterCommand(new GetTxResultCmd());
            program.RegisterCommand(new GetGenesisContractAddressCmd());
            
            // Start the CLI
            program.StartRepl();
        }

        private static void RegisterNetworkCommands(AElfCliProgram program)
        {
            program.RegisterCommand(new GetPeersCmd());
            program.RegisterCommand(new GetCommandsCmd());
        }

        private static void RegisterAccountCommands(AElfCliProgram program)
        {
            program.RegisterCommand(new AccountCmd());
        }
    }
}