using System;
using System.Collections.Generic;

namespace AElf.CLI
{
    class Program
    {
        private static List<string> _commands = new List<string>();
        private static readonly RpcCalls Rpc = new RpcCalls();
        
        private const string InvalidCommandError = "***** ERROR: INVALID COMMAND - SEE USAGE *****";
        private const string InvalidParamsError = "***** ERROR: INVALID PARAMETERS - SEE USAGE *****";
        private const string CommandNotAvailable = "***** ERROR: COMMAND NO LONGER AVAILABLE - PLEASE RESTART *****";
        
        public static void Main(string[] args)
        {
            _commands = Rpc.GetCommands().Result;

            Console.WriteLine("------------------------------------------------\n" +
                              "Welcome to AElf!");
            
            while (true)
            {
                Menu();
            }
        }

        private static void Menu()
        {
            Console.WriteLine("------------------------------------------------\n" +
                              "Usage: command_name <param1> <param2> ...\n" +
                              "To Quit: Type 'Quit'\n\n" +
                              "Available commands:\n");
            
            foreach (var comm in _commands)
            {
                Console.WriteLine(comm);
            }
            
            Console.WriteLine();

            string exec = Console.ReadLine();
            exec = exec ?? string.Empty;
            string[] tokens = exec.Split();
            string choice = tokens[0];

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
                case "Quit":
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
        }
    }
}