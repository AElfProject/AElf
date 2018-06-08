using System;
using System.Collections.Generic;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;

namespace AElf.CLI
{
    public class AElfCliProgram
    {
        private static List<string> _commands = new List<string>();
        private static readonly RpcCalls Rpc = new RpcCalls();
        
        private const string InvalidCommandError = "***** ERROR: INVALID COMMAND - SEE USAGE *****";
        private const string InvalidParamsError = "***** ERROR: INVALID PARAMETERS - SEE USAGE *****";
        private const string CommandNotAvailable = "***** ERROR: COMMAND NO LONGER AVAILABLE - PLEASE RESTART *****";
        private const string ErrorLoadingCommands = "***** ERROR: COULD NOT LOAD COMMANDS - PLEASE RESTART SERVER *****";
        
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