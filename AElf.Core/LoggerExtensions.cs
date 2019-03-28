using System;
using Microsoft.Extensions.Logging;

Microsoft.Extensions.Logging;

namespace AElf
{
    public static class LoggerExtensions
    {
        public static void LogTrace(this ILogger logger, Func<String> func)
        {
#if DEBUG
            logger.Log
    (func());
#endif
        }
    }
}