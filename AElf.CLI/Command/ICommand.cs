using System.Collections.Generic;

namespace AElf.CLI.Command
{
    /// <summary>
    /// The ICommand is the interface of all client commands.
    /// The command is stateless. The Process method will change or invoke
    /// instances in context, but never modify itself.
    /// 
    /// NOTE: It could be a sub-command, like `git commit`, etc.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Process the user imput arguments and returns outputs.
        ///
        /// The CommandException could be thrown if some error occurred.
        /// </summary>
        /// <param name="args">
        /// The command arguments.
        /// </param>
        /// <param name="context">
        /// The command context. The Context contains all states of AElf CLI program.
        /// </param>
        /// <returns>
        /// The outputs to print.
        /// </returns>
        string Process(IEnumerable<string> args, AElfClientProgramContext context);
        
        /// <summary>
        /// Get the usage of this command. The useage will be print when Process raise a CommandException.
        /// </summary>
        string Usage { get; }
    }
}