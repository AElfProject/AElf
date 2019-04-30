using System;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace AElf
{
    public static class LoggerExtensions
    {
        public static void LogTrace(this ILogger logger, Func<String> func)
        {
#if DEBUG
            logger.LogTrace(func());
#endif
        }
    }
}