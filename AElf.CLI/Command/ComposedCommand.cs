using System.Collections.Generic;
using System.Linq;

namespace AElf.CLI.Command
{
    public abstract class ComposedCommand : ICommand
    {
        public abstract string Process(IEnumerable<string> args, AElfClientProgramContext context);
        public abstract string Usage { get; }
        
        protected static string DispatchToSubCommands(IEnumerable<string> args,
            AElfClientProgramContext context,
            IDictionary<string, ICommand> sub_commands)
        {
            var cmd = args.First();
            try
            {
                return sub_commands[cmd].Process(args.Skip(1), context);
            }
            catch (KeyNotFoundException ex)
            {
                throw new CommandException($"Cannot find command {cmd}");
            }
        }
    }
}