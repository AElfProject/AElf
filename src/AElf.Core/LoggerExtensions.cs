using System;
using Microsoft.Extensions.Logging;

namespace AElf;

public static class LoggerExtensions
{
    public static void LogTrace(this ILogger logger, Func<string> func)
    {
#if DEBUG
        logger.LogTrace(func());
#endif
    }
}