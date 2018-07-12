using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Types.CSharp;

namespace AElf.CLI.Command
{
    /// <summary>
    /// Composed command can compose many sub-commands together.
    /// </summary>
    public class ComposedCommand : ICommand
    {
        public IDictionary<string, ICommand> SubCommands;
        protected string CurrentCommandName;

        public string Process(IEnumerable<string> args, AElfClientProgramContext context)
        {
            var cmd = args.First();
            try
            {
                return SubCommands[cmd].Process(args.Skip(1), context);
            }
            catch (KeyNotFoundException ex)
            {
                throw new CommandException($"Cannot find command {cmd}");
            }
        }


        public string Usage
        {
            get
            {
                var stringBuilder = new StringBuilder();
                BuildUsageRecursively(0, this, stringBuilder);
                return stringBuilder.ToString();
            }
        }


        private static void AppendIndent(StringBuilder stringBuilder, int indentLevel)
        {
            for (int i = 0; i < indentLevel; ++i)
            {
                stringBuilder.Append("    ");
            }
        }
        
        private static void BuildUsageRecursively(int indentLevel, ICommand cmd, StringBuilder stringBuilder)
        {
            if (cmd is ComposedCommand composedCommand)
            {
                if (composedCommand.CurrentCommandName.Length != 0)
                {
                    AppendIndent(stringBuilder, indentLevel++);
                    stringBuilder.Append(composedCommand.CurrentCommandName);
                    stringBuilder.Append("\n");
                }

                foreach (var subCmd in composedCommand.SubCommands.Values)
                {
                    BuildUsageRecursively(indentLevel, subCmd, stringBuilder);
                }
            }
            else
            {
                foreach (var line in cmd.Usage.Split('\n'))
                {
                    AppendIndent(stringBuilder, indentLevel);
                    stringBuilder.Append(line);
                    stringBuilder.Append("\n");
                }
            }
        }
    }
}