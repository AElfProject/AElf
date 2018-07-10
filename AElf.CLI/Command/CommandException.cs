using System;

namespace AElf.CLI.Command
{
    public class CommandException : Exception
    {
        public CommandException(string message) : base(message)
        {
        }
    }
}