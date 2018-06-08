using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AElf.CLI.Command;
using AElf.CLI.Parsing;
using AElf.CLI.Screen;
using AElf.CLI.Wallet;

namespace AElf.CLI
{
    public class AElfCliProgram
    {
        private static readonly RpcCalls Rpc = new RpcCalls();
        
        private static List<CliCommandDefinition> _commands = new List<CliCommandDefinition>();
        
        private const string InvalidCommandError = "***** ERROR: INVALID COMMAND - SEE USAGE *****";
        private const string InvalidParamsError = "***** ERROR: INVALID PARAMETERS - SEE USAGE *****";
        private const string CommandNotAvailable = "***** ERROR: COMMAND NO LONGER AVAILABLE - PLEASE RESTART *****";
        private const string ErrorLoadingCommands = "***** ERROR: COULD NOT LOAD COMMANDS - PLEASE RESTART SERVER *****";
        
        private const string ExitReplCommand = "quit";
        
        private readonly ScreenManager _screenManager;
        private readonly CommandParser _cmdParser;
        private readonly AccountManager _accountManager;
        
        public AElfCliProgram(ScreenManager screenManager, CommandParser cmdParser, AccountManager accountManager)
        {
            _screenManager = screenManager;
            _cmdParser = cmdParser;
            _accountManager = accountManager;
            
            _commands = new List<CliCommandDefinition>();
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
                CliCommandDefinition def = GetCommandDefinition(parsedCmd.Command);

                if (def == null)
                {
                    _screenManager.PrintCommandNotFount(command);
                }
                else
                {
                    ProcessCommand(parsedCmd, def);
                }
            }
        }

        private void ProcessCommand(CmdParseResult parsedCmd, CliCommandDefinition def)
        {
            string error = def.Validate(parsedCmd);

            if (!string.IsNullOrEmpty(error))
            {
                _screenManager.PrintError(error);
            }
            else
            {
                // Execute
                // 2 cases : RPC command, Local command (like account management)
                if (def.IsLocal)
                {
                    _accountManager.ProcessCommand(parsedCmd);
                }
                else
                {
                    // RPC
                    
                }
            }
        }

        private CliCommandDefinition GetCommandDefinition(string commandName)
        {
            var cmd = _commands.FirstOrDefault(c => c.Name.Equals(commandName));
            return cmd;
        }

        public void RegisterCommand(CliCommandDefinition cmd)
        {
            _commands.Add(cmd);
        }

        private void Stop()
        {
            
        }
    }
}