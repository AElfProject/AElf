using System;
using System.Collections.Generic;
using System.Linq;
using NServiceKit.Redis;

namespace AElf.CLI.Command
{
    public class RootCommand : ComposedCommand
    {
        private class GetCommands : ICommand
        {
            private readonly IDictionary<string, ICommand> _commands;

            public GetCommands(IDictionary<string, ICommand> commands)
            {
                _commands = commands;
            }

            public string Process(IEnumerable<string> args, AElfClientProgramContext context)
            {
                if (args.Count() != 0)
                {
                    throw new CommandException("get_commands does not need any params");
                }

                return String.Join("\n", _commands.Keys);
            }

            public string Usage { get; } = "get_commands";
        }

        public RootCommand()
        {
            SubCommands = new Dictionary<string, ICommand>();
            SubCommands["get_commands"] = new GetCommands(SubCommands);
            SubCommands["account"] = new AccountCommand();
            CurrentCommandName = "";
        }
    }
}