using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Launcher;

internal partial class Program
{
    protected virtual async Task<FlowBehavior> HandleExceptionWhileStartingNode(Exception ex)
    {
        ILogger<Program> logger = NullLogger<Program>.Instance;
        if (logger == NullLogger<Program>.Instance)
            Console.WriteLine(ex);
        logger.LogCritical(ex, "program crashed");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        };
    }
}