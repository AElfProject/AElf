using AElf.CLI.Parsing;
using AElf.CLI.Screen;

namespace AElf.CLI
{
    class Program
    {
        public static void Main(string[] args)
        {
            ScreenManager screenManager = new ScreenManager();
            CommandParser parser = new CommandParser();
            
            AElfCliProgram program = new AElfCliProgram(screenManager, parser);
            
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