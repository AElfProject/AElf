using System;

namespace AElf.CLI.Command
{
    public class CommandException : Exception
    {
        public CommandException(string message) : base(message)
        {
        }
    }

    public class InvalidNumberArgumentsException : CommandException
    {
        public InvalidNumberArgumentsException() : base("Invalid number of arguments")
        {
        }
    }
}