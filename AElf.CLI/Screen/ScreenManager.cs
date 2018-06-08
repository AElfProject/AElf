using System;

namespace AElf.CLI.Screen
{
    public class ScreenManager
    {
        private const string CliPrefix = "> AElf$ ";
        private const string Usage = "Usage: command_name <param1> <param2> ...";
        private const string Quit = "To Quit: Type 'Quit'";
        
        public void PrintHeader()
        {
            PrintSeparator();
            Console.WriteLine("Welcome to AElf!");
            PrintSeparator();
        }

        public void PrintSeparator()
        {
            Console.WriteLine("------------------------------------------------");
        }

        public void PrintUsage()
        {
            Console.WriteLine("Use \"getcommands\" to print available commands");
            Console.WriteLine(Usage);
            Console.WriteLine(Quit);
        }

        public string GetCommand()
        {
            string command = null;
            
            while (string.IsNullOrWhiteSpace(command))
            {
                Console.Write(CliPrefix);
                command = Console.ReadLine();
            }
            
            return command;
        }

        public void PrintCommandNotFount(string command)
        {
            Console.WriteLine(command + " : command not found");
        }

        public void PrintError(string error)
        {
            Console.WriteLine(error);
        }
    }
}