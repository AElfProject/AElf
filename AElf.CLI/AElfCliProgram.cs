using System;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;

namespace AElf.CLI
{
    public class AElfCliProgram
    {
        private const string ExitReplCommand = "quit";
        
        private readonly ScreenManager _screenManager;
        private readonly CommandParser _cmdParser;
        
        public AElfCliProgram(ScreenManager screenManager, CommandParser cmdParser)
        {
            _screenManager = screenManager;
            _cmdParser = cmdParser;
        }

        public void StartRepl()
        {
            _screenManager.PrintHeader();
            _screenManager.PrintUsage();
            
            while (true)
            {
                string command = _screenManager.GetCommand();

                // stop the repl if "quit", "Quit", "QuiT", ... is encountered
                if (command.Equals(ExitReplCommand, StringComparison.OrdinalIgnoreCase))
                {
                    Stop();
                    break;
                }

                CmdParseResult parsedCmd = _cmdParser.Parse(command);
                Console.WriteLine("Parsed : " + parsedCmd.Command);

                foreach (var strcmd in parsedCmd.Args)
                {
                    Console.WriteLine(" arg: " + strcmd);
                }
            }
        }

        private void Stop()
        {
            
        }
    }
}