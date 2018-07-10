using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.CLI.Command
{
    public class RootCommand : ComposedCommand
    {
        public static readonly Dictionary<string, ICommand> Commands = new Dictionary<string, ICommand>();

        static RootCommand()
        {
            Commands["get_commands"] = new GetCommands();
            Commands["account"] = new AccountCommand();
        }

        private class GetCommands : ICommand
        {
            public string Process(IEnumerable<string> args, AElfClientProgramContext context)
            {
                if (args.Count() != 0)
                {
                    throw new CommandException("get_commands does not need any params");
                }

                return String.Join("\n", Commands.Keys);
            }

            public string Usage { get; } = "get_commands";
        }

        public override string Process(IEnumerable<string> args, AElfClientProgramContext context)
        {
            return DispatchToSubCommands(args, context, Commands);
        }

        public override string Usage { get; } = "";
    }
}