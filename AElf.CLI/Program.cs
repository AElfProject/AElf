using System;
using System.Collections.Generic;

namespace AElf.CLI
{
    class Program
    {
        private static List<string> _commands = new List<string>();
        private static readonly RpcCalls Rpc = new RpcCalls();
        
        public static void Main(string[] args)
        {
            _commands = Rpc.GetCommands().Result;
            while (true)
            {
                Menu();
            }
        }

        private static void Menu()
        {
            Console.WriteLine("------------------------------------------------\n" +
                              "Welcome to AElf!\n" +
                              "------------------------------------------------\n" +
                              "Usage: command_name param1 param2 ...\n\n" +
                              "Available commands:\n");
            
            foreach (var comm in _commands)
            {
                Console.WriteLine(comm);
            }
            
            Console.WriteLine("0. Quit\n");

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
                        Console.WriteLine("\n***** ERROR: INVALID PARAMETERS - SEE USAGE *****\n");
                    }

                    break;
                case "0":
                    Environment.Exit(0);
                    break;
                case "Quit":
                    Environment.Exit(0);
                    break;
                default:
                    break;
            }
        }

        private static void GetPeers(string numPeers)
        {
            Console.WriteLine("\nList of Peers:");
            List<string> peers = Rpc.GetPeers(numPeers).Result;
            foreach (var p in peers)
            {
                Console.WriteLine(p);
            }
            Console.WriteLine("\n");
        }
    }
}