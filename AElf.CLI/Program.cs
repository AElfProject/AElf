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

            AElfCliProgram program = new AElfCliProgram(screenManager, parser, manager);

            // Register local commands

            program.RegisterCommand(new AccountCmd());
            program.RegisterCommand(new GetPeersCmd());
            program.RegisterCommand(new GetCommandsCmd());
            program.RegisterCommand(new GetIncrementCmd());
            program.RegisterCommand(new SendTransactionCmd());

            // Start the CLI
            program.StartRepl();
        }
        /*private static void Menu()
        {
            if (_commands.Count == 0)
                Console.WriteLine(ErrorLoadingCommands);
            
            foreach (var comm in _commands)
            {
                Console.WriteLine(comm);
            }
            
            Console.WriteLine();

            Console.Write(CliPrefix);
            string exec = Console.ReadLine();
            
            
            exec = exec ?? string.Empty;
            string[] tokens = exec.Split();
            string choice = tokens[0].ToLower();

            switch (choice)
            {
                case "get_peers":
                    if (tokens.Length > 1)
                    {
                        GetPeers(tokens[1]);
                    }
                    else
                    {
                        Console.WriteLine("\n" + InvalidParamsError + "\n");
                    }

                    break;
                case "help":
                    Console.WriteLine("\n" + Usage + "\n");
                    break;
                case "quit":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("\n" + InvalidCommandError + "\n");
                    break;
            }
        }

        private static void GetPeers(string numPeers)
        {
            List<string> peers;
            ushort n;
            bool parsed = ushort.TryParse(numPeers, out n);
            if (!parsed || n == 0)
            {
                Console.WriteLine("\n" + InvalidParamsError + "\n");
                return;
            }

            peers = Rpc.GetPeers(n).Result;
            
            if (peers.Count == 0)
            {
                Console.WriteLine("\n" + CommandNotAvailable + "\n");
                return;
            }
            
            Console.WriteLine("\nList of Peers:");
            foreach (var p in peers)
            {
                Console.WriteLine(p);
            }
            Console.WriteLine();
        }*/
    }
}