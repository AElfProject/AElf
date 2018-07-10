using System.Collections.Generic;

namespace AElf.CLI.Command
{
    public interface ICommand
    {
        string Process(IEnumerable<string> args, AElfClientProgramContext context);
        
        string Usage { get; }
    }
}